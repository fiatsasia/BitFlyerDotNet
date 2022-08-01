//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

#pragma warning disable CS8603
#pragma warning disable CS8604

namespace BitFlyerDotNet.Trading;

class BfxMarket
{
    public bool IsInitialized { get; private set; }
    public event EventHandler<BfxOrderChangedEventArgs>? OrderChanged;

    BitFlyerClient _client;
    string _productCode;
    BfxPrivateDataSource _pds;
    BfxConfiguration _config;

    ConcurrentDictionary<string, BfxTransaction> _tx = new();

    public BfxMarket(BitFlyerClient client, string productCode, BfxPrivateDataSource pds, BfxConfiguration config)
    {
        _client = client;
        _productCode = productCode;
        _pds = pds;
        _config = config;
    }

    public async Task InitializeAsync()
    {
        if (!_client.IsAuthenticated)
        {
            throw new InvalidOperationException("Client is not authorized. To Authenticate first.");
        }
        if (IsInitialized)
        {
            throw new InvalidOperationException($"Market '{_productCode}' is already initialized.");
        }

        IsInitialized = true;

        await foreach (var ctx in _pds.GetOrderServerContextsAsync(_productCode, BfOrderState.Active, 0))
        {
            _tx.TryAdd(ctx.OrderAcceptanceId, new BfxTransaction(_client, ctx, _config));
        }
    }

    public async Task<string> PlaceOrderAsync<TOrder>(TOrder order, CancellationToken ct) where TOrder : IBfOrder
    {
        // Sometimes child order event arrives before send order process completion.
        var tx = new BfxTransaction(_client, _pds.CreateOrderContext(_productCode), _config);
        tx.OrderChanged += OnOrderChanged;
        var acceptanceId = await tx.PlaceOrderAsync(order, ct);
        if (string.IsNullOrEmpty(acceptanceId))
        {
            return default;
        }
        Log.Debug($"Transaction id:{acceptanceId} opened");
        tx = _tx.GetOrAdd(acceptanceId, tx);
        _pds.TryRegisterOrderContext(_productCode, acceptanceId, tx.GetOrderContext().Update(order, acceptanceId));
        return acceptanceId;
    }

    public async Task CancelOrderAsync(string acceptanceId, CancellationToken ct) => await _tx[acceptanceId].CancelOrderAsync(ct);

    public void OnOrderEvent(IBfOrderEvent e)
    {
        var acceptanceId = e.GetAcceptanceId();
        var tx = _tx.GetOrAdd(acceptanceId, _ =>
        {
            var tx = new BfxTransaction(_client, _pds.GetOrCreateOrderContext(_productCode, acceptanceId), _config);
            tx.OrderChanged += OnOrderChanged;
            Log.Debug($"Transaction id:{acceptanceId} opened");
            return tx;
        });
        _pds.TryRegisterOrderContext(_productCode, acceptanceId, tx.GetOrderContext().Update(e));
        tx.OnOrderEvent(e);

        if (!tx.GetOrderContext().IsActive)
        {
            _tx.TryRemove(acceptanceId, out tx);
            Log.Debug($"Transaction id:{acceptanceId} closed");
        }
    }

    void OnOrderChanged(object sender, BfxOrderChangedEventArgs e) => OrderChanged?.Invoke(sender, e);
}
