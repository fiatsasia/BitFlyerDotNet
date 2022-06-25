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
    public BfxConfiguration Config { get; }
    public bool IsInitialized => _markets.Count() > 0;

    CompositeDisposable _disposables = new();
    Dictionary<string, BfxMarket> _markets = new();
    BitFlyerClient _client;
    RealtimeSourceFactory _rts;
    Dictionary<string, BfxMarketDataSource> _mdss = new();

    #region Initialize and Finalize
    public BfxApplication()
    {
        Config = new BfxConfiguration();
        _client = new BitFlyerClient().AddTo(_disposables);
        _rts = new RealtimeSourceFactory();
    }

    public BfxApplication(string key, string secret)
    {
        Config = new BfxConfiguration();
        _client = new BitFlyerClient(key, secret).AddTo(_disposables);
        _rts = new RealtimeSourceFactory(key, secret);
    }

    public BfxApplication(BfxConfiguration config, string key, string secret)
    {
        Config = config;
        _client = new BitFlyerClient(key, secret).AddTo(_disposables);
        _rts = new RealtimeSourceFactory(key, secret);
    }

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
            market.TradeChanged += OnTradeChanged;
            _markets.Add(productCode, market);
        }

        if (_client.IsAuthenticated)
        {
            _rts.GetParentOrderEventsSource().Subscribe(e => _markets[e.ProductCode].OnParentOrderEvent(e)).AddTo(_disposables);
            _rts.GetChildOrderEventsSource().Subscribe(e => _markets[e.ProductCode].OnChildOrderEvent(e)).AddTo(_disposables);
        }
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

    public async Task<BfxMarket> InitializeAsync(string productCode)
    {
        if (!IsInitialized)
        {
            await InitializeAsync();
        }

        if (!_markets.TryGetValue(productCode, out var market))
        {
            throw new ArgumentException();
        }

        if (!market.IsInitialized)
        {
            await market.InitializeAsync();
        }

        return market;
    }
    #endregion Initialize and Finalize

    #region Events
    public event EventHandler<BfxOrderChangedEventArgs>? OrderChanged;
    public event EventHandler<BfxPositionChangedEventArgs>? PositionChanged;
    public event EventHandler<BfxTradeChangedEventArgs>? TradeChanged;

    private void OnTradeChanged(object sender, BfxTradeChangedEventArgs e) => TradeChanged?.Invoke(sender, e);
    #endregion Events

    public async Task<BfxMarket> GetMarketAsync(string productCode)
    {
        if (!_markets.TryGetValue(productCode, out var market))
        {
            market = await InitializeAsync(productCode);
        }
        else if (!market.IsInitialized)
        {
            await market.InitializeAsync();
        }

        return market;
    }

    async Task VerifyOrder(BfChildOrder order)
    {
        if (order.Size > Config.OrderSizeMax[order.ProductCode])
        {
            throw new ArgumentException("Order size exceeds the maximum size.");
        }

        var mds = await GetMarketDataSourceAsync(order.ProductCode);
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

    async Task VerifyOrder(BfParentOrder order)
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

        var mds = await GetMarketDataSourceAsync(order.Parameters[0].ProductCode);
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

    public async Task<BfxTransaction> PlaceOrderAsync(BfChildOrder order)
    {
        await VerifyOrder(order);
        return await (await GetMarketAsync(order.ProductCode)).PlaceOrderAsync(order);
    }

    public async Task<BfxTransaction> PlaceOrderAsync(BfParentOrder order)
    {
        await VerifyOrder(order);
        return await (await GetMarketAsync(order.Parameters[0].ProductCode)).PlaceOrderAsync(order);
    }

    public async Task<BfxMarketDataSource> GetMarketDataSourceAsync(string productCode)
    {
        if (!_mdss.TryGetValue(productCode, out var mds))
        {
            mds = new BfxMarketDataSource(productCode, _client, _rts);
            _mdss.Add(productCode, mds);
            await mds.InitializeAsync();
        }
        return mds;
    }

    private async IAsyncEnumerable<BfxTrade> GetTradesAsync(string productCode, BfOrderState orderState, int count, bool linkChildToParent, Func<BfxTrade, bool> predicate)
    {
#pragma warning disable CS8604
        var trades = new Dictionary<string, BfxTrade>();

        // Get child orders
        await foreach (var childOrder in _client.GetChildOrdersAsync(productCode, orderState, count, 0, e => true))
        {
            var trade = new BfxTrade(productCode);
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
            var trade = new BfxTrade(productCode);
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

        await foreach (var trade in GetTradesAsync(productCode, BfOrderState.Unknown, count, true, e => true))
        {
            yield return new BfxOrder(trade);
        }
    }

    public IAsyncEnumerable<BfxOrder> GetRecentOrdersAsync(int count) => GetRecentOrdersAsync(Config.DefaultProductCode, count);
}
