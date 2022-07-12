//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

public class BfxMarket : IDisposable
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

    internal async Task InitializeAsync()
    {
        if (!_client.IsAuthenticated)
        {
            throw new InvalidOperationException($"Client is not authorized. To Authenticate first.");
        }

        IsInitialized = true;

        await foreach (var ctx in _pds.GetActiveOrders(_productCode))
        {
            var tx = new BfxTransaction(_client, ctx, _config);
            _oid2tid[ctx.OrderAcceptanceId] = tx.Id;
            _tx.TryAdd(tx.Id, tx);
        }
    }

    internal void OnChildOrderEvent(BfChildOrderEvent e)
    {
        var tid = _oid2tid.GetOrAdd(e.ChildOrderAcceptanceId, _ =>
        {
            var tx = new BfxTransaction(_client, _pds.GetOrCreateOrderContext(_productCode, e.ChildOrderAcceptanceId), _config);
            tx.OrderChanged += OnOrderChanged;
            return tx.Id;
        });
        var tx = _tx[tid];
        tx.GetOrderContext().Update(e);
        tx.OnChildOrderEvent(e);

        if (_pds.FindParent(e.ChildOrderAcceptanceId, out var parent))
        {
            parent.UpdateChild(e);
        }

        if (tx.GetOrderContext().OrderState != BfOrderState.Active)
        {
            _oid2tid.TryRemove(e.ChildOrderAcceptanceId, out _);
            _tx.TryRemove(tid, out tx);
            Log.Debug($"Transaction cid:{e.ChildOrderAcceptanceId} closed");
        }
    }

    internal void OnParentOrderEvent(BfParentOrderEvent e)
    {
        var tid = _oid2tid.GetOrAdd(e.ParentOrderAcceptanceId, _ =>
        {
            var tx = new BfxTransaction(_client, _pds.GetOrCreateOrderContext(_productCode, e.ParentOrderAcceptanceId), _config);
            tx.OrderChanged += OnOrderChanged;
            return tx.Id;
        });
        var tx = _tx[tid];
        tx.GetOrderContext().Update(e);
        tx.OnParentOrderEvent(e);

        if (tx.GetOrderContext().OrderState != BfOrderState.Active)
        {
            _oid2tid.TryRemove(e.ParentOrderAcceptanceId, out _);
            _tx.TryRemove(tid, out tx);
            Log.Debug($"Transaction pid:{e.ParentOrderAcceptanceId} closed");
        }
    }

    public async Task<string> PlaceOrderAsync(BfChildOrder order, CancellationTokenSource cts)
    {
        // Sometimes child order event arraives before send order process completes.
        var tx = new BfxTransaction(_client, _pds.CreateOrderContext(_productCode).Update(order), _config);
        tx.OrderChanged += OnOrderChanged;
        var oid = await tx.PlaceOrderAsync(order, cts);
        if (string.IsNullOrEmpty(oid))
        {
            return default;
        }
        var tid = _oid2tid.GetOrAdd(oid, _ => { _tx.TryAdd(tx.Id, tx); return tx.Id; });
        _tx[tid].GetOrderContext().Update(order);
        return oid;
    }

    public async Task<string> PlaceOrderAsync(BfParentOrder order, CancellationTokenSource cts)
    {
        // Sometimes parent order event arraives before send order process completes.
        var tx = new BfxTransaction(_client, _pds.CreateOrderContext(_productCode).Update(order), _config);
        tx.OrderChanged += OnOrderChanged;
        var oid = await tx.PlaceOrdertAsync(order, cts);
        if (string.IsNullOrEmpty(oid))
        {
            return default;
        }
        var tid = _oid2tid.GetOrAdd(oid, _ => { _tx.TryAdd(tx.Id, tx); return tx.Id; });
        _tx[tid].GetOrderContext().Update(order);
        return oid;
    }

    public async Task CancelOrderAsync(string acceptanceId, CancellationTokenSource cts)
    {
        var tx = _tx[_oid2tid[acceptanceId]];
        if (tx.GetOrderContext().HasChildren)
        {
            await tx.CancelParentOrderAsync(cts);
        }
        else
        {
            await tx.CancelChildOrderAsync(cts);
        }
    }

    void OnOrderChanged(object sender, BfxOrderChangedEventArgs e) => OrderChanged?.Invoke(sender, e);
}
