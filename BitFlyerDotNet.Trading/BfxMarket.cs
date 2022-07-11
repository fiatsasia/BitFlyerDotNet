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

    ConcurrentDictionary<string, BfxTransaction> _tx = new();

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
            _tx.TryAdd(ctx.OrderAcceptanceId, new BfxTransaction(_client, ctx, _config));
        }
    }

    internal void OnChildOrderEvent(BfChildOrderEvent e)
    {
        var tx = _tx.AddOrUpdate(e.ChildOrderAcceptanceId,
            id =>
            {
                var tx = new BfxTransaction(_client, _pds.GetOrCreateOrderContext(_productCode, id).Update(e), _config);
                tx.OrderChanged += OnOrderChanged;
                return tx.OnChildOrderEvent(e);
            },
            (_, tx) =>
            {
                tx.GetOrderContext().Update(e);
                return tx.OnChildOrderEvent(e);
            }
        );

        if (_pds.FindParent(e.ChildOrderAcceptanceId, out var parent))
        {
            parent.UpdateChild(e);
        }

        if (tx.GetOrderContext().OrderState != BfOrderState.Active)
        {
            _tx.TryRemove(e.ChildOrderAcceptanceId, out tx);
            Log.Debug($"Transaction cid:{e.ChildOrderAcceptanceId} closed");
        }
    }

    internal void OnParentOrderEvent(BfParentOrderEvent e)
    {
        var tx = _tx.AddOrUpdate(e.ParentOrderAcceptanceId,
            id =>
            {
                var tx = new BfxTransaction(_client, _pds.GetOrCreateOrderContext(_productCode, id).Update(e), _config);
                tx.OrderChanged += OnOrderChanged;
                return tx.OnParentOrderEvent(e);
            },
            (_, tx) =>
            {
                tx.GetOrderContext().Update(e);
                return tx.OnParentOrderEvent(e);
            }
        );
        if (tx.GetOrderContext().OrderState != BfOrderState.Active)
        {
            _tx.TryRemove(e.ParentOrderAcceptanceId, out tx);
            Log.Debug($"Transaction pid:{e.ParentOrderAcceptanceId} closed");
        }
    }

    public async Task<BfxTransaction?> PlaceOrderAsync(BfChildOrder order)
    {
        // Sometimes child order event arraives before send order process completes.
        var tx = new BfxTransaction(_client, _pds.CreateOrderContext(_productCode).Update(order), _config);
        tx.OrderChanged += OnOrderChanged;
        var id = await tx.PlaceOrderAsync(order);
        if (string.IsNullOrEmpty(id))
        {
            return default;
        }
        return _tx.AddOrUpdate(id, _ => tx, (_, tx) => { tx.GetOrderContext().Update(order); return tx; }); // Sometimes order event arrives first
    }

    public async Task<BfxTransaction?> PlaceOrderAsync(BfParentOrder order)
    {
        // Sometimes parent order event arraives before send order process completes.
        var tx = new BfxTransaction(_client, _pds.CreateOrderContext(_productCode).Update(order), _config);
        tx.OrderChanged += OnOrderChanged;
        var id = await tx.PlaceOrdertAsync(order);
        if (string.IsNullOrEmpty(id))
        {
            return default;
        }
        return _tx.AddOrUpdate(id, _ => tx, (_, tx) => { tx.GetOrderContext().Update(order); return tx; }); // Sometimes order event arrives first
    }

    void OnOrderChanged(object sender, BfxOrderChangedEventArgs e) => OrderChanged?.Invoke(sender, e);
}
