//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxApplication : IDisposable
    {
        public bool IsInitialized => _markets.Count() > 0;

        CompositeDisposable _disposables = new();
        Dictionary<string, BfxMarket> _markets = new();

        BitFlyerClient _client;
        RealtimeSourceFactory _rts;

        #region Initialize and Finalize
        public BfxApplication()
        {
            _client = new BitFlyerClient().AddTo(_disposables);
            _rts = new RealtimeSourceFactory();
        }

        public BfxApplication(string key, string secret)
        {
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

            var availableMarkets = (await _client.GetMarketsAsync(CancellationToken.None)).GetContent();
            await _rts.TryOpenAsync();

            foreach (var productCode in availableMarkets.Select(e => !string.IsNullOrEmpty(e.Alias) ? e.Alias : e.ProductCode))
            {
                var market = new BfxMarket(_client, productCode);
                market.OrderChanged += OnOrderChanged;
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

        public async Task InitializeAsync(string productCode)
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
        }
        #endregion Initialize and Finalize

        #region Events
        public event EventHandler<BfxOrderChangedEventArgs>? OrderChanged;
        public event EventHandler<BfxPositionChangedEventArgs>? PositionChanged;

        private void OnOrderChanged(object sender, BfxOrderChangedEventArgs e) => OrderChanged?.Invoke(sender, e);
        #endregion Events

        public async Task<BfxMarket> GetMarketAsync(string productCode)
        {
            if (!_markets.TryGetValue(productCode, out var market))
            {
                await InitializeAsync(productCode);
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

        public Task<IEnumerable<BfxOrderStatus>> GetActiveOrdersAsync(string productCode)
        {
            throw new NotImplementedException();
        }
    }
}
