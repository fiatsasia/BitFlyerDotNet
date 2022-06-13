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
    public BfTicker Ticker { get; internal set; }

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
            throw new InvalidOperationException($"Client is not authenticated. To Authenticate first.");
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
        VerifyOrder(order);
        var tx = new BfxTransaction(_client, _productCode, _config);
        tx.TransactionChanged += OnTransactionChanged;
        var id = await tx.PlaceOrderAsync(order);
        return _orderTransactions.AddOrUpdate(id, _ => tx, (_, tx) => tx.Update(order));
    }

    public async Task<BfxTransaction> PlaceOrderAsync(BfParentOrder order)
    {
        // Sometimes parent order event arraives before send order process completes.
        VerifyOrder(order);
        var tx = new BfxTransaction(_client, _productCode, _config);
        tx.TransactionChanged += OnTransactionChanged;
        var id = await tx.PlaceOrdertAsync(order);
        return _orderTransactions.AddOrUpdate(id, _ => tx, (_, tx) => tx.Update(order));
    }

    void VerifyOrder(BfChildOrder order)
    {
        if (order.Size > _config.OrderSizeMax[order.ProductCode])
        {
            throw new ArgumentException("Order size exceeds the maximum size.");
        }

        if (_config.OrderPriceLimitter && order.ChildOrderType == BfOrderType.Limit)
        {
#pragma warning disable CS8629
            if (order.Side == BfTradeSide.Buy && order.Price.Value > Ticker.BestAsk)
            {
                throw new ArgumentException("Buy order price is above best ask price.");
            }
            else if (order.Side == BfTradeSide.Sell && order.Price.Value < Ticker.BestBid)
            {
                throw new ArgumentException("Sell order price is below best bid price.");
            }
#pragma warning restore CS8629
        }
    }

    void VerifyOrder(BfParentOrder order)
    {
        foreach (var child in order.Parameters)
        {
            if (child.Size > _config.OrderSizeMax[_productCode])
            {
                throw new ArgumentException("Order size exceeds the maximum size.");
            }
        }

        var children = order.OrderMethod switch
        {
            BfOrderType.IFD => order.Parameters.Take(1),
            BfOrderType.OCO => order.Parameters.Take(2),
            BfOrderType.IFDOCO => order.Parameters.Take(1),
            _ => throw new ArgumentException()
        };
        foreach (var child in children)
        {
            if (_config.OrderPriceLimitter && child.ConditionType == BfOrderType.Limit)
            {
#pragma warning disable CS8629
                if (child.Side == BfTradeSide.Buy && child.Price.Value > Ticker.BestAsk)
                {
                    throw new ArgumentException("Buy order price is above best ask price.");
                }
                else if (child.Side == BfTradeSide.Sell && child.Price.Value < Ticker.BestBid)
                {
                    throw new ArgumentException("Sell order price is below best bid price.");
                }
#pragma warning restore CS8629
            }
        }
    }

    private void OnTransactionChanged(object sender, BfxTransactionChangedEventArgs e)
    {
        switch (e.EvenetType)
        {
        }
    }

    public async IAsyncEnumerable<BfxTrade> GetTradesAsync(BfOrderState orderState, int count, bool linkChildToParent, Func<BfxTrade, bool> predicate)
    {
#pragma warning disable CS8604
        var trades = new Dictionary<string, BfxTrade>();

        // Get child orders
        await foreach (var childOrder in _client.GetChildOrdersAsync(_productCode, orderState, count, 0, e => true))
        {
            var trade = new BfxTrade(_productCode);
            var execs = await _client.GetPrivateExecutionsAsync(_productCode, childOrderId: childOrder.ChildOrderId);
            trade.Update(childOrder, execs);
            if (!predicate(trade))
            {
                break;
            }
            trades.Add(trade.OrderAcceptanceId, trade);
        }

        // Get parent orders
        await foreach (var parentOrder in _client.GetParentOrdersAsync(_productCode, orderState, count, 0, e => true))
        {
            var trade = new BfxTrade(_productCode);
            var parentOrderDetail = await _client.GetParentOrderAsync(_productCode, parentOrderId: parentOrder.ParentOrderId);
            trade.Update(parentOrder, parentOrderDetail);
            if (!predicate(trade))
            {
                break;
            }
            trades.Add(trade.OrderAcceptanceId, trade);
        }

        // Link child to parent
        if (linkChildToParent)
        {
            foreach (var parentOrder in trades.Values.Where(e => (e.OrderType != BfOrderType.Market && e.OrderType != BfOrderType.Limit)))
            {
                foreach (var childOrder in await _client.GetChildOrdersAsync(_productCode, parentOrderId: parentOrder.OrderId))
                {
                    var execs = await _client.GetPrivateExecutionsAsync(_productCode, childOrderId: childOrder.ChildOrderId);
                    trades[parentOrder.OrderAcceptanceId].UpdateChild(childOrder, execs);
                    trades.Remove(childOrder.ChildOrderAcceptanceId);
                }
            }
        }

        foreach (var trade in trades.Values)
        {
            yield return trade;
        }
#pragma warning restore CS8604
    }
}
