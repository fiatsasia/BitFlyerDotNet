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
    BfxConfiguration _config;

    ConcurrentDictionary<string, BfxTransaction> _orderTransactions = new();

    public BfxMarket(BitFlyerClient client, string productCode, BfxConfiguration config)
    {
        _client = client;
        _productCode = productCode;
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

        // Load active child orders and their executions
        var childOrders = await _client.GetChildOrdersAsync(_productCode, orderState: BfOrderState.Active);
        foreach (var childOrder in childOrders)
        {
            var execs = await _client.GetPrivateExecutionsAsync(_productCode, childOrderId: childOrder.ChildOrderId);
            _orderTransactions.AddOrUpdate(childOrder.ChildOrderAcceptanceId,
                _ => { var tx = new BfxTransaction(_client, _productCode, _config); tx.OrderChanged += OnOrderChanged; return tx.Update(childOrder, execs); },
                (_, tx) => tx.Update(childOrder, execs)
            );
        }

        // Load active parent orders, their children and executions.
        await foreach (var parentOrder in _client.GetParentOrdersAsync(_productCode, BfOrderState.Active, 0, 0, e => true))
        {
            var parentOrderDetail = await _client.GetParentOrderAsync(_productCode, parentOrderId: parentOrder.ParentOrderId);
            _orderTransactions.AddOrUpdate(parentOrder.ParentOrderAcceptanceId,
                _ => { var tx = new BfxTransaction(_client, _productCode, _config); tx.OrderChanged += OnOrderChanged; return tx.Update(parentOrder, parentOrderDetail); },
                (_, tx) => tx.Update(parentOrder, parentOrderDetail)
            );

            foreach (var childOrder in await _client.GetChildOrdersAsync(_productCode, parentOrderId: parentOrder.ParentOrderId))
            {
                var execs = await _client.GetPrivateExecutionsAsync(_productCode, childOrderId: childOrder.ChildOrderId);
                _orderTransactions.AddOrUpdate(childOrder.ChildOrderAcceptanceId,
                    _ => { var tx = new BfxTransaction(_client, _productCode, _config); tx.OrderChanged += OnOrderChanged; return tx.Update(childOrder, execs); },
                    (_, tx) => tx.Update(childOrder, execs)
                );
            }
        }
    }

    internal void OnParentOrderEvent(BfParentOrderEvent e)
    {
        _orderTransactions.AddOrUpdate(e.ParentOrderAcceptanceId,
            _ => { var tx = new BfxTransaction(_client, _productCode, _config); tx.OrderChanged += OnOrderChanged; return tx.OnParentOrderEvent(e); },
            (_, tx) => tx.OnParentOrderEvent(e)
        );
    }

    internal void OnChildOrderEvent(BfChildOrderEvent e)
    {
        _orderTransactions.AddOrUpdate(e.ChildOrderAcceptanceId,
            _ => { var tx = new BfxTransaction(_client, _productCode, _config); tx.OrderChanged += OnOrderChanged; return tx.OnChildOrderEvent(e); },
            (_, tx) => tx.OnChildOrderEvent(e));
    }

    public async Task<BfxTransaction?> PlaceOrderAsync(BfChildOrder order)
    {
        // Sometimes child order event arraives before send order process completes.
        var tx = new BfxTransaction(_client, _productCode, _config);
        tx.OrderChanged += OnOrderChanged;
        var id = await tx.PlaceOrderAsync(order);
        if (string.IsNullOrEmpty(id))
        {
            return default;
        }
        return _orderTransactions.AddOrUpdate(id, _ => tx, (_, tx) => tx.Update(order));
    }

    public async Task<BfxTransaction?> PlaceOrderAsync(BfParentOrder order)
    {
        // Sometimes parent order event arraives before send order process completes.
        var tx = new BfxTransaction(_client, _productCode, _config);
        tx.OrderChanged += OnOrderChanged;
        var id = await tx.PlaceOrdertAsync(order);
        if (string.IsNullOrEmpty(id))
        {
            return default;
        }
        return _orderTransactions.AddOrUpdate(id, _ => tx, (_, tx) => tx.Update(order));
    }

    void OnOrderChanged(object sender, BfxOrderChangedEventArgs e) => OrderChanged?.Invoke(sender, e);
}
