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

    public event EventHandler<BfxPositionChangedEventArgs>? PositionChanged
    {
        add { _positions.PositionChanged += value; }
        remove { _positions.PositionChanged -= value; }
    }
    public event EventHandler<BfxTradeChangedEventArgs>? TradeChanged;

    BitFlyerClient _client;
    string _productCode;
    BfxConfiguration _config;

    BfxPositionManager _positions = new();
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

        // Load active positions from market
        if (_productCode == BfProductCode.FX_BTC_JPY)
        {
            _positions.Update(await _client.GetPositionsAsync(BfProductCode.FX_BTC_JPY));
        }

        // Load active child orders and their executions
        var childOrders = await _client.GetChildOrdersAsync(_productCode, orderState: BfOrderState.Active);
        foreach (var childOrder in childOrders)
        {
            var execs = await _client.GetPrivateExecutionsAsync(_productCode, childOrderId: childOrder.ChildOrderId);
            _orderTransactions.AddOrUpdate(childOrder.ChildOrderAcceptanceId,
                _ => { var tx = new BfxTransaction(_client, _productCode, _config); tx.TransactionChanged += OnTransactionChanged; return tx.Update(childOrder, execs); },
                (_, tx) => tx.Update(childOrder, execs)
            );
        }

        // Load active parent orders, their children and executions.
        await foreach (var parentOrder in _client.GetParentOrdersAsync(_productCode, BfOrderState.Active, 0, 0, e => true))
        {
            var parentOrderDetail = await _client.GetParentOrderAsync(_productCode, parentOrderId: parentOrder.ParentOrderId);
            _orderTransactions.AddOrUpdate(parentOrder.ParentOrderAcceptanceId,
                _ => { var tx = new BfxTransaction(_client, _productCode, _config); tx.TransactionChanged += OnTransactionChanged; return tx.Update(parentOrder, parentOrderDetail); },
                (_, tx) => tx.Update(parentOrder, parentOrderDetail)
            );

            foreach (var childOrder in await _client.GetChildOrdersAsync(_productCode, parentOrderId: parentOrder.ParentOrderId))
            {
                var execs = await _client.GetPrivateExecutionsAsync(_productCode, childOrderId: childOrder.ChildOrderId);
                _orderTransactions.AddOrUpdate(childOrder.ChildOrderAcceptanceId,
                    _ => { var tx = new BfxTransaction(_client, _productCode, _config); tx.TransactionChanged += OnTransactionChanged; return tx.Update(childOrder, execs); },
                    (_, tx) => tx.Update(childOrder, execs)
                );
                _positions.Update(execs);
            }
        }
    }

    internal void OnParentOrderEvent(BfParentOrderEvent e)
    {
        _orderTransactions.AddOrUpdate(e.ParentOrderAcceptanceId,
            _ => { var tx = new BfxTransaction(_client, _productCode, _config); tx.TransactionChanged += OnTransactionChanged; return tx.OnParentOrderEvent(e); },
            (_, tx) => tx.OnParentOrderEvent(e)
        );

        switch (e.EventType)
        {
            case BfOrderEventType.Trigger:
            case BfOrderEventType.Complete:
#pragma warning disable CS8604
                _orderTransactions.AddOrUpdate(e.ChildOrderAcceptanceId,
                    _ => { var tx = new BfxTransaction(_client, _productCode, _config); tx.TransactionChanged += OnTransactionChanged; return tx.OnTriggerOrCompleteEvent(e); },
                    (_, tx) => tx.OnTriggerOrCompleteEvent(e)
                );
#pragma warning restore CS8604
                break;
        }
    }

    internal void OnChildOrderEvent(BfChildOrderEvent e)
    {
        if (e.ProductCode == BfProductCode.FX_BTC_JPY && e.EventType == BfOrderEventType.Execution)
        {
            Task.Run(async () =>
            {
                _positions.Update(e);
                _positions.Update(await _client.GetPositionsAsync(e.ProductCode));
            });
        }

        _orderTransactions.AddOrUpdate(e.ChildOrderAcceptanceId,
            _ => { var tx = new BfxTransaction(_client, _productCode, _config); tx.TransactionChanged += OnTransactionChanged; return tx.OnChildOrderEvent(e); },
            (_, tx) => tx.OnChildOrderEvent(e));
    }

    public async Task<BfxTransaction> PlaceOrderAsync(BfChildOrder order)
    {
        // Sometimes child order event arraives before send order process completes.
        var tx = new BfxTransaction(_client, _productCode, _config);
        tx.TransactionChanged += OnTransactionChanged;
        var id = await tx.PlaceOrderAsync(order);
        return _orderTransactions.AddOrUpdate(id, _ => tx, (_, tx) => tx.Update(order));
    }

    public async Task<BfxTransaction> PlaceOrderAsync(BfParentOrder order)
    {
        // Sometimes parent order event arraives before send order process completes.
        var tx = new BfxTransaction(_client, _productCode, _config);
        tx.TransactionChanged += OnTransactionChanged;
        var id = await tx.PlaceOrdertAsync(order);
        return _orderTransactions.AddOrUpdate(id, _ => tx, (_, tx) => tx.Update(order));
    }

    private void OnTransactionChanged(object sender, BfxTransactionChangedEventArgs e)
    {
        switch (e.EvenetType)
        {
        }
    }
}
