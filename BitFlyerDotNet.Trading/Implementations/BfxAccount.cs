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
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public partial class BfxAccount : IDisposable
    {
        public BitFlyerClient Client { get; private set; }
        public RealtimeSourceFactory RealtimeSource { get; private set; }

        public event EventHandler<BfxPositionChangedEventArgs>? PositionChanged;

        CompositeDisposable _disposables = new CompositeDisposable();
        Dictionary<string, BfxMarket> _markets = new();
        public BfxPositions Positions { get; } = new BfxPositions();

        public BfxAccount(string apiKey, string apiSecret)
        {
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiKey))
            {
                Client = new BitFlyerClient().AddTo(_disposables);
                RealtimeSource = RealtimeSourceFactory.Singleton;
            }
            else
            {
                Client = new BitFlyerClient(apiKey, apiSecret).AddTo(_disposables);
                RealtimeSource = RealtimeSourceFactory.Singleton;
                RealtimeSource.Authenticate(apiKey, apiSecret);
            }
            RealtimeSource.ConnectionResumed += OnRealtimeConnectionResumed;
        }

        public BfxAccount() : this(string.Empty, string.Empty)
        {
        }

        private void OnRealtimeConnectionResumed()
        {
            // ポジション情報の再読み込み後、遅延したイベントを受信しないのか？
            Positions.Update(Client.GetPositions("FX_BTC_JPY").GetContent());
        }

        public void Dispose()
        {
            _disposables.DisposeReverse();
        }

        async Task InitializeMarketsAsync()
        {
            if (_markets.Count > 0)
            {
                return;
            }

            /*var result = await Client.GetAvailableMarketsAsync(CancellationToken.None);
            result.ForEach(e =>
            {
                _markets.Add(e.ProductCode, new BfxMarket(this, e.ProductCode).AddTo(_disposables));
                _marketSymbols.Add(e.Symbol, e.ProductCode);
            });*/
        }

        public async Task OpenAsync()
        {
            await InitializeMarketsAsync();
            await RealtimeSource.TryOpenAsync();

            if (!Client.IsAuthenticated)
            {
                return;
            }

            Positions.Update((await Client.GetPositionsAsync("FX_BTC_JPY", CancellationToken.None)).GetContent());
            RealtimeSource.GetChildOrderEventsSource().Subscribe(coe =>
            {
                _markets[coe.ProductCode].ForwardChildOrderEvents(coe);
                if (coe.ProductCode == "FX_BTC_JPY" && coe.EventType == BfOrderEventType.Execution)
                {
                    Positions.Update(coe).ForEach(e => PositionChanged?.Invoke(this, new BfxPositionChangedEventArgs(coe.EventDate, e)));
                }
            });

            RealtimeSource.GetParentOrderEventsSource().Subscribe(poe =>
            {
                _markets[poe.ProductCode].ForwardParentOrderEvents(poe);
            });
        }

        public async Task<BfxMarket> GetMarketAsync(string productCode)
        {
            await InitializeMarketsAsync();
            return _markets[productCode];
        }
    }
}
