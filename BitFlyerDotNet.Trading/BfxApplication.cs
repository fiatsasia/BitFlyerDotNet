//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

#pragma warning disable CS8629

namespace BitFlyerDotNet.Trading;

public class BfxApplication : IDisposable
{
    #region Properties and fields
    public Ulid Id { get; } = Ulid.NewUlid();
    public RealtimeSourceFactory RealtimeSource => _rts;
    public BfxConfiguration Config { get; }
    public bool IsInitialized => _markets.Count() > 0;

    CompositeDisposable _disposables = new();
    BitFlyerClient _client;
    RealtimeSourceFactory _rts;
    Dictionary<string, BfxMarket> _markets = new();
    Dictionary<string, BfxMarketDataSource> _mds = new();
    BfxPrivateDataSource _pds;
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
        _pds = new(_client);

        VerifyOrderAsync = VerifyOrderDefaultAsync;
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

        foreach (var mi in availableMarkets)
        {
            var market = new BfxMarket(_client, mi.ProductCode, _pds, Config);
            market.OrderChanged += (sender, e) => OrderChanged?.Invoke(sender, e);
            _markets[mi.ProductCode] = market;
            _mds[mi.ProductCode] = new BfxMarketDataSource(mi.ProductCode, _client, _rts);
            if (!string.IsNullOrEmpty(mi.Alias))
            {
                _markets[mi.Alias] = _markets[mi.ProductCode];
                _mds[mi.Alias] = _mds[mi.ProductCode];
            }
        }

        if (_client.IsAuthenticated)
        {
            _rts.GetParentOrderEventsSource().Subscribe(e => _markets[e.ProductCode].OnParentOrderEvent(e)).AddTo(_disposables);
            _rts.GetChildOrderEventsSource().Subscribe(async e =>
            {
                _markets[e.ProductCode].OnChildOrderEvent(e);
                if (e.ProductCode == BfProductCode.FX_BTC_JPY && e.EventType == BfOrderEventType.Execution)
                {
                    // child order subscription is scheduled on Rx default thread queueing scheduler
                    await foreach (var pos in _pds.UpdatePositionAsync(e))
                    {
                        PositionChanged?.Invoke(this, new BfxPositionChangedEventArgs(pos, await _pds.GetTotalPositionSizeAsync()));
                    }
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

        if (productCode == BfProductCode.FX_BTC_JPY)
        {
            await _pds.InitializePositionsAsync(BfProductCode.FX_BTC_JPY);
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

    #region Verify order
    public Func<IBfOrder, Task> VerifyOrderAsync { get; set; }
    public async Task VerifyOrderDefaultAsync(IBfOrder order)
    {
        var productCode = order.GetProductCode();
        if (!_markets.ContainsKey(productCode))
        {
            throw new ArgumentException($"order: Unknown product code '{productCode}'.");
        }

        var ticker = (await GetMarketDataSourceAsync(productCode)).Ticker;
        switch (order)
        {
            case BfChildOrder childOrder: VerifyOrder(childOrder, ticker); break;
            case BfParentOrder parentOrder: VerifyOrder(parentOrder, ticker); break;
            default: throw new ArgumentException();
        }
    }

    void VerifyOrder(BfChildOrder order, BfTicker ticker)
    {
        if (order.Size > Config.OrderSizeMax[order.ProductCode])
        {
            throw new ArgumentException("child order: Order size exceeds the maximum size.");
        }

        switch (order.ChildOrderType)
        {
            case BfOrderType.Market:
                break;

            case BfOrderType.Limit:
                if (order.Price.Value != Math.Round(order.Price.Value, BfProductCode.GetPriceDecimals(order.ProductCode)))
                {
                    throw new ArgumentException($"child order: The accuracy of order price varies.");
                }
                if (order.Side == BfTradeSide.Buy && order.Price.Value > ticker.BestAsk)
                {
                    throw new ArgumentException($"child order: Buy order price {order.Price.Value} is above best ask price {ticker.BestAsk}.");
                }
                else if (order.Side == BfTradeSide.Sell && order.Price.Value < ticker.BestBid)
                {
                    throw new ArgumentException($"child order: Sell order price {order.Price.Value} is below best bid price {ticker.BestBid}.");
                }
                break;
        }
    }

    void VerifyOrder(BfParentOrder parentOrder, BfTicker ticker)
    {
        for (var childIndex = 0; childIndex < parentOrder.Parameters.Count; childIndex++)
        {
            var order = parentOrder.Parameters[childIndex];

            // Check decimals
            if (order.Price.HasValue && order.Price.Value != Math.Round(order.Price.Value, BfProductCode.GetPriceDecimals(order.ProductCode)))
            {
                throw new ArgumentException($"parent order: The accuracy of order price varies.");
            }
            if (order.TriggerPrice.HasValue && order.TriggerPrice.Value != Math.Round(order.TriggerPrice.Value, BfProductCode.GetPriceDecimals(order.ProductCode)))
            {
                throw new ArgumentException($"child order: The accuracy of trigger price varies.");
            }
            if (order.Offset.HasValue && order.Offset.Value != Math.Round(order.Offset.Value, BfProductCode.GetPriceDecimals(order.ProductCode)))
            {
                throw new ArgumentException($"child order: The accuracy of trail offset varies.");
            }

            if (parentOrder.OrderMethod == BfOrderType.OCO || ((parentOrder.OrderMethod == BfOrderType.IFD || parentOrder.OrderMethod == BfOrderType.IFDOCO) && childIndex == 0))
            {
                switch (order.ConditionType)
                {
                    case BfOrderType.Limit:
                        if (order.Side == BfTradeSide.Buy && order.Price.Value > ticker.BestAsk)
                        {
                            throw new ArgumentException($"parent order: Buy order price {order.Price.Value} is above best ask price {ticker.BestAsk}.");
                        }
                        else if (order.Side == BfTradeSide.Sell && order.Price.Value < ticker.BestBid)
                        {
                            throw new ArgumentException($"parent order: Sell order price {order.Price.Value} is below best bid price {ticker.BestBid}.");
                        }
                        break;

                    case BfOrderType.Stop:
                    case BfOrderType.StopLimit:
                        if (order.Side == BfTradeSide.Buy && order.TriggerPrice.Value < ticker.BestBid)
                        {
                            throw new ArgumentException("parent order: Buy trigger price is below best bid price.");
                        }
                        else if (order.Side == BfTradeSide.Sell && order.TriggerPrice.Value > ticker.BestAsk)
                        {
                            throw new ArgumentException("parent order: Sell trigger price is above best ask price.");
                        }
                        break;
                }
            }
        }
    }
    #endregion Verify order

    #region Ordering
    public event EventHandler<BfxOrderChangedEventArgs>? OrderChanged;
    public async Task<string> PlaceOrderAsync<TOrder>(TOrder order, CancellationToken ct = default) where TOrder : IBfOrder
    {
        var productCode = order.GetProductCode();
        if (!IsMarketInitialized(productCode))
        {
            await InitializeMarketAsync(productCode);
        }

        if (!IsMarketDataSourceInitialized(productCode))
        {
            await InitializeMarketDataSourceAsync(productCode);
        }

        if (!Config.IsVerifyDisabled)
        {
            order.Verify();
            await VerifyOrderAsync?.Invoke(order);
        }

        return await _markets[productCode].PlaceOrderAsync(order, ct);
    }

    public async Task CancelOrderAsync(string productCode, string acceptanceId, CancellationToken ct = default)
    {
        if (!IsMarketInitialized(productCode))
        {
            await InitializeMarketAsync(productCode);
        }

        if (!IsMarketDataSourceInitialized(productCode))
        {
            await InitializeMarketDataSourceAsync(productCode); // To subscribe order events
        }

        await _markets[productCode].CancelOrderAsync(acceptanceId, ct);
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

    public async IAsyncEnumerable<BfxOrder> GetActiveOrdersAsync(string productCode)
    {
        if (!IsMarketInitialized(productCode))
        {
            await InitializeMarketAsync(productCode);
        }

        var ctxs = _pds.GetOrderCacheContexts(productCode).Where(e => e.IsActive).ToDictionary(e => e.OrderAcceptanceId, e => e);

        // Attach children to parent
        foreach (var pctx in ctxs.Values.Where(e => e.HasChildren).ToList())
        {
            var parentOrder = new BfxOrder(pctx);
            for (var childIndex = 0; childIndex < pctx.Children.Count; childIndex++)
            {
                var cctx = pctx.Children[childIndex];
                if (!string.IsNullOrEmpty(cctx.OrderAcceptanceId) && ctxs.TryGetValue(cctx.OrderAcceptanceId, out cctx))
                {
                    parentOrder.ReplaceChild(childIndex, cctx);
                    ctxs.Remove(cctx.OrderAcceptanceId);
                }
            }
            ctxs.Remove(pctx.OrderAcceptanceId);
            yield return parentOrder;
        }

        // Enumerates independent child orders
        foreach (var ctx in ctxs.Values)
        {
            yield return new(ctx);
        }
    }

    public IAsyncEnumerable<BfxOrder> GetRecentOrdersAsync(string productCode, int count)
        => _pds.GetOrderServerContextsAsync(productCode, BfOrderState.Unknown, count).Where(e => !e.HasChildren).Select(e => new BfxOrder(e));

    #region Manage positions
    public event EventHandler<BfxPositionChangedEventArgs>? PositionChanged;

    public IAsyncEnumerable<BfxPosition> GetActivePositions(string productCode) => _pds.GetActivePositionsAsync(productCode);
    #endregion Manage positions
}
