//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

public class BfxApplication : IDisposable
{
    #region Properties and fields
    public RealtimeSourceFactory RealtimeSource => _rts;
    public BfxConfiguration Config { get; }
    public bool IsInitialized => _markets.Count() > 0;

    CompositeDisposable _disposables = new();
    BitFlyerClient _client;
    RealtimeSourceFactory _rts;
    Dictionary<string, BfxMarket> _markets = new();
    Dictionary<string, BfxMarketDataSource> _mds = new();
    BfxPositionManager _positions = new();
    #endregion Properties and fields

    #region Initialize and Finalize
    public BfxApplication(BfxConfiguration config, string key, string secret)
    {
        Config = config;
        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(secret))
        {
            _client = new BitFlyerClient(key, secret).AddTo(_disposables);
            _rts = new RealtimeSourceFactory(key, secret).AddTo(_disposables);
        }
        else
        {
            _client = new BitFlyerClient().AddTo(_disposables);
            _rts = new RealtimeSourceFactory();
        }
        VerifyChildOrder = VerifyOrder;
        VerifyParentOrder = VerifyOrder;
    }

    public BfxApplication() : this(new BfxConfiguration(), string.Empty, string.Empty) { }

    public BfxApplication(string key, string secret) : this(new BfxConfiguration(), key, secret) { }

    public void Dispose()
    {
        _disposables.DisposeReverse();
    }

    public async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        var availableMarkets = await _client.GetMarketsAsync();
        await _rts.TryOpenAsync();

        foreach (var productCode in availableMarkets.Select(e => !string.IsNullOrEmpty(e.Alias) ? e.Alias : e.ProductCode))
        {
            var market = new BfxMarket(_client, productCode, Config);
            market.OrderChanged += (sender, e) => OrderChanged?.Invoke(sender, e);
            _markets[productCode] = market;
            _mds[productCode] = new BfxMarketDataSource(productCode, _client, _rts);
        }

        if (_client.IsAuthenticated)
        {
            _rts.GetParentOrderEventsSource().Subscribe(e => _markets[e.ProductCode].OnParentOrderEvent(e)).AddTo(_disposables);
            _positions = new(await _client.GetPositionsAsync(BfProductCode.FX_BTC_JPY));
            _rts.GetChildOrderEventsSource().Subscribe(e =>
            {
                _markets[e.ProductCode].OnChildOrderEvent(e);
                if (e.ProductCode == BfProductCode.FX_BTC_JPY && e.EventType == BfOrderEventType.Execution)
                {
                    // child order subscription is scheduled on Rx default thread queueing scheduler
                    _positions.Update(e).ForEach(pos => PositionChanged?.Invoke(this, new BfxPositionChangedEventArgs(pos, _positions.TotalSize)));
                }
            }).AddTo(_disposables);
        }
    }

    // This will be called when suspended WebSocket
    public Task RefreshAsync()
    {
        throw new NotImplementedException();
    }

    public async Task AuthenticateAsync(string key, string secret)
    {
        if (_client.IsAuthenticated)
        {
            return;
        }

        if (!IsInitialized)
        {
            await InitializeAsync();
        }

        _client.Authenticate(key, secret);
        await Task.Run(() => _rts.Authenticate(key, secret));

        _rts.GetParentOrderEventsSource().Subscribe(e => _markets[e.ProductCode].OnParentOrderEvent(e)).AddTo(_disposables);
        _rts.GetChildOrderEventsSource().Subscribe(e => _markets[e.ProductCode].OnChildOrderEvent(e)).AddTo(_disposables);
    }

    public bool IsMarketInitialized(string productCode) => IsInitialized ? _markets[productCode].IsInitialized : false;

    public async Task InitializeMarketAsync(string productCode)
    {
        if (!IsInitialized)
        {
            await InitializeAsync();
        }

        if (!_markets[productCode].IsInitialized)
        {
            await _markets[productCode].InitializeAsync();
        }
    }

    public bool IsMarketDataSourceInitialized(string productCode) => IsInitialized ? _mds[productCode].IsInitialized : false;

    public async Task InitializeMarketDataSourceAsync(string productCode)
    {
        if (!IsInitialized)
        {
            await InitializeAsync();
        }

        if (!_mds[productCode].IsInitialized)
        {
            await _mds[productCode].InitializeAsync();
        }
    }
    #endregion Initialize and Finalize

    #region Events
    public event EventHandler<BfxOrderChangedEventArgs>? OrderChanged;
    public event EventHandler<BfxPositionChangedEventArgs>? PositionChanged;
    #endregion Events

    #region Ordering
    public void VerifyOrder(BfChildOrder order)
    {
        if (order.Size > Config.OrderSizeMax[order.ProductCode])
        {
            throw new ArgumentException("Order size exceeds the maximum size.");
        }

        var mds = _mds[order.ProductCode];
        if (Config.OrderPriceLimitter && order.ChildOrderType == BfOrderType.Limit)
        {
#pragma warning disable CS8629
            if (order.Side == BfTradeSide.Buy && order.Price.Value > mds.Ticker.BestAsk)
            {
                throw new ArgumentException("Buy order price is above best ask price.");
            }
            else if (order.Side == BfTradeSide.Sell && order.Price.Value < mds.Ticker.BestBid)
            {
                throw new ArgumentException("Sell order price is below best bid price.");
            }
#pragma warning restore CS8629
        }
    }

    public void VerifyOrder(BfParentOrder order)
    {
        foreach (var child in order.Parameters)
        {
            if (child.Size > Config.OrderSizeMax[order.Parameters[0].ProductCode])
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

        var mds = _mds[order.Parameters[0].ProductCode];
        foreach (var child in children)
        {
            if (Config.OrderPriceLimitter && child.ConditionType == BfOrderType.Limit)
            {
#pragma warning disable CS8629
                if (child.Side == BfTradeSide.Buy && child.Price.Value > mds.Ticker.BestAsk)
                {
                    throw new ArgumentException("Buy order price is above best ask price.");
                }
                else if (child.Side == BfTradeSide.Sell && child.Price.Value < mds.Ticker.BestBid)
                {
                    throw new ArgumentException("Sell order price is below best bid price.");
                }
#pragma warning restore CS8629
            }
        }
    }

    public Action<BfChildOrder> VerifyChildOrder { get; set; }
    public Action<BfParentOrder> VerifyParentOrder { get; set; }

    public async Task<BfxTransaction?> PlaceOrderAsync(BfChildOrder order)
    {
        if (!IsMarketInitialized(order.ProductCode))
        {
            await InitializeMarketAsync(order.ProductCode);
        }

        if (!IsMarketDataSourceInitialized(order.ProductCode))
        {
            await InitializeMarketDataSourceAsync(order.ProductCode);
        }

        VerifyChildOrder.Invoke(order);
        return await _markets[order.ProductCode].PlaceOrderAsync(order);
    }

    public async Task<BfxTransaction?> PlaceOrderAsync(BfParentOrder order)
    {
        if (!IsMarketInitialized(order.Parameters[0].ProductCode))
        {
            await InitializeMarketAsync(order.Parameters[0].ProductCode);
        }

        if (!IsMarketDataSourceInitialized(order.Parameters[0].ProductCode))
        {
            await InitializeMarketDataSourceAsync(order.Parameters[0].ProductCode);
        }

        VerifyParentOrder.Invoke(order);
        return await _markets[order.Parameters[0].ProductCode].PlaceOrderAsync(order);
    }
    #endregion Ordering

    public async Task<BfxMarketDataSource> GetMarketDataSourceAsync(string productCode)
    {
        if (!IsMarketDataSourceInitialized(productCode))
        {
            await InitializeMarketDataSourceAsync(productCode);
        }

        return _mds[productCode];
    }

    private async IAsyncEnumerable<BfxOrderStatus> GetOrdersAsync(string productCode, BfOrderState orderState, int count, bool linkChildToParent, Func<BfxOrderStatus, bool> predicate)
    {
#pragma warning disable CS8604
        var trades = new Dictionary<string, BfxOrderStatus>();

        // Get child orders
        await foreach (var childOrder in _client.GetChildOrdersAsync(productCode, orderState, count, 0, e => true))
        {
            var trade = new BfxOrderStatus(productCode);
            var execs = await _client.GetPrivateExecutionsAsync(productCode, childOrderId: childOrder.ChildOrderId);
            trade.Update(childOrder, execs);
            if (!predicate(trade))
            {
                break;
            }
            trades.Add(trade.OrderAcceptanceId, trade);
        }

        // Get parent orders
        await foreach (var parentOrder in _client.GetParentOrdersAsync(productCode, orderState, count, 0, e => true))
        {
            var trade = new BfxOrderStatus(productCode);
            var parentOrderDetail = await _client.GetParentOrderAsync(productCode, parentOrderId: parentOrder.ParentOrderId);
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
                foreach (var childOrder in await _client.GetChildOrdersAsync(productCode, parentOrderId: parentOrder.OrderId))
                {
                    var execs = await _client.GetPrivateExecutionsAsync(productCode, childOrderId: childOrder.ChildOrderId);
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

    public async IAsyncEnumerable<BfxOrder> GetRecentOrdersAsync(string productCode, int count)
    {
        if (!_client.IsAuthenticated)
        {
            throw new BitFlyerUnauthorizedException();
        }

        await foreach (var trade in GetOrdersAsync(productCode, BfOrderState.Unknown, count, true, e => true))
        {
            yield return new BfxOrder(trade);
        }
    }

    public IAsyncEnumerable<BfxOrder> GetRecentOrdersAsync(int count) => GetRecentOrdersAsync(Config.DefaultProductCode, count);

    public IEnumerable<BfxPosition> GetActivePositions() => _positions.GetActivePositions();
}
