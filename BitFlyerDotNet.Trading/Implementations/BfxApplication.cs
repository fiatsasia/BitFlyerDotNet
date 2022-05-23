//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxApplication : IDisposable
    {
        public event EventHandler<BfxOrderChangedEventArgs>? OrderChanged;
        public event EventHandler<BfxPositionChangedEventArgs>? PositionChanged;

        CompositeDisposable _disposables = new();
        Dictionary<string, BfxMarket2> _markets = new();

        string _key;
        string _secret;
        BitFlyerClient _client;
        RealtimeSourceFactory _rts;

        public BfxApplication(string key, string secret)
        {
            _key = key;
            _secret = secret;
            _client = new BitFlyerClient(key, secret).AddTo(_disposables);
            _rts = new RealtimeSourceFactory(key, secret);
        }

        public void Dispose()
        {
            _disposables.DisposeReverse();
        }

        public async Task InitializeAsync()
        {
            await _rts.TryOpenAsync();
            if (_client.IsAuthenticated)
            {
                await Task.Run(() => _rts.Authenticate(_key, _secret));
            }

            var availableMarkets = (await _client.GetMarketsAsync(CancellationToken.None)).GetContent();
            foreach (var productCode in availableMarkets.Select(e => !string.IsNullOrEmpty(e.Alias) ? e.Alias : e.ProductCode))
            {
                var market = new BfxMarket2(_client, _rts, productCode);
                market.OrderChanged += OnOrderChanged;
                _markets.Add(productCode, market);
            }

            if (_client.IsAuthenticated)
            {
                _rts.GetParentOrderEventsSource().Subscribe(e => _markets[e.ProductCode].OnParentOrderEvent(e)).AddTo(_disposables);
                _rts.GetChildOrderEventsSource().Subscribe(e => _markets[e.ProductCode].OnChildOrderEvent(e)).AddTo(_disposables);

            }
        }

        private void OnOrderChanged(object sender, BfxOrderChangedEventArgs e) => OrderChanged?.Invoke(sender, e);

        public async Task InitializeProductAsync(string productCode)
        {
            if (_markets.Count == 0)
            {
                await InitializeAsync();
            }

            if (!_markets.TryGetValue(productCode, out var market))
            {
                throw new ArgumentException();
            }

            await market.InitializeAsync();
        }

        public BfxAccount GetAccount()
        {
            throw new NotImplementedException();
        }

        public BfxMarket GetMarket(string productCode)
        {
            throw new NotImplementedException();
        }

        public Task<IBfxTransaction> PlaceOrderAsync(IBfxOrder order) => throw new NotImplementedException();

    }
}
