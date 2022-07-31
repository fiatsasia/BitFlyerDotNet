//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

class BfxMarket : IDisposable
{
    public bool IsInitialized { get; private set; }
    public event EventHandler<BfxOrderChangedEventArgs>? OrderChanged;

    BitFlyerClient _client;
    string _productCode;
    BfxPrivateDataSource _pds;
    BfxConfiguration _config;

    ConcurrentDictionary<Ulid, BfxTransaction> _tx = new();
    ConcurrentDictionary<string, Ulid> _oid2tid = new();

    public BfxMarket(BitFlyerClient client, string productCode, BfxPrivateDataSource pds, BfxConfiguration config)
    {
        _client = client;
        _productCode = productCode;
        _pds = pds;
        _config = config;
    }

    public void Dispose()
    {
    }

    public async Task InitializeAsync()
    {
        if (!_client.IsAuthenticated)
        {
            throw new InvalidOperationException($"Client is not authorized. To Authenticate first.");
        }

        IsInitialized = true;

        await foreach (var ctx in _pds.GetOrderServerContextsAsync(_productCode, BfOrderState.Active, 0))
        {
            var tx = new BfxTransaction(_client, ctx, _config);
            _oid2tid[ctx.OrderAcceptanceId] = tx.Id;
            _tx.TryAdd(tx.Id, tx);
        }
    }

    public void OnOrderEvent(IBfOrderEvent e)
    {
        var acceptanceId = e.GetAcceptanceId();
        var tid = _oid2tid.GetOrAdd(acceptanceId, _ =>
        {
            var tx = new BfxTransaction(_client, _pds.GetOrCreateOrderContext(_productCode, acceptanceId), _config);
            tx.OrderChanged += OnOrderChanged;
            _tx.TryAdd(tx.Id, tx);
            Log.Debug($"Transaction tid:{tx.Id} opened");
            return tx.Id;
        });
        var tx = _tx[tid];
        _pds.TryRegisterOrderContext(_productCode, acceptanceId, tx.GetOrderContext());

        tx.GetOrderContext().Update(e);
        tx.OnOrderEvent(e);

        if (!tx.GetOrderContext().IsActive)
        {
            _oid2tid.TryRemove(acceptanceId, out _);
            _tx.TryRemove(tx.Id, out tx);
            Log.Debug($"Transaction tid:{tx.Id} closed");
        }

    }

    public async Task<string> PlaceOrderAsync<TOrder>(TOrder order, CancellationToken ct) where TOrder : IBfOrder
    {
        // Sometimes child order event arraives before send order process completes.
        var tx = new BfxTransaction(_client, _pds.CreateOrderContext(_productCode).Update(order), _config);
        tx.OrderChanged += OnOrderChanged;
        var acceptanceId = await tx.PlaceOrderAsync(order, ct);
        if (string.IsNullOrEmpty(acceptanceId))
        {
            return default;
        }
        var tid = _oid2tid.GetOrAdd(acceptanceId, _ =>
        {
            _tx.TryAdd(tx.Id, tx);
            Log.Debug($"Transaction tid:{tx.Id} opened");
            return tx.Id;
        });
        _pds.TryRegisterOrderContext(_productCode, acceptanceId, _tx[tid].GetOrderContext().Update(order));
        return acceptanceId;
    }

    public async Task CancelOrderAsync(string acceptanceId, CancellationToken ct)
        => await _tx[_oid2tid[acceptanceId]].CancelOrderAsync(ct);

    void OnOrderChanged(object sender, BfxOrderChangedEventArgs e) => OrderChanged?.Invoke(sender, e);
}
