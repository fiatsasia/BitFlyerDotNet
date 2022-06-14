﻿//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

public class BfxApplication : IDisposable
{
    BfxConfiguration Config { get; }
    public bool IsInitialized => _markets.Count() > 0;

    CompositeDisposable _disposables = new();
    Dictionary<string, BfxMarket> _markets = new();

    BitFlyerClient _client;
    RealtimeSourceFactory _rts;

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
            market.Ticker = await _client.GetTickerAsync(productCode);
            _rts.GetTickerSource(productCode).Subscribe(ticker => { market.Ticker = ticker; });
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

    public async Task<BfxTransaction> PlaceOrderAsync(BfParentOrder order) => await (await GetMarketAsync(order.Parameters[0].ProductCode)).PlaceOrderAsync(order);
    public async Task<BfxTransaction> PlaceOrderAsync(BfChildOrder order) => await (await GetMarketAsync(order.ProductCode)).PlaceOrderAsync(order);

    public Task<BfxMarketDataSource> GetMarketDataSourceAsync(string productCode)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<BfxOrder> GetRecentOrdersAsync(string productCode, int count)
    {
        if (!_client.IsAuthenticated)
        {
            throw new BitFlyerUnauthorizedException();
        }

        var market = await GetMarketAsync(productCode);
        await foreach (var trade in market.GetTradesAsync(BfOrderState.Unknown, count, true, e => true))
        {
            yield return new BfxOrder(trade);
        }
    }
}
