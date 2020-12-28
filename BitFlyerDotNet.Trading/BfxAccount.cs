//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Collections.Generic;
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

        public event EventHandler<BfxPositionEventArgs>? PositionChanged;

        CompositeDisposable _disposables = new CompositeDisposable();
        Dictionary<string, BfProductCode> _marketSymbols = new Dictionary<string, BfProductCode>();
        Dictionary<BfProductCode, BfxMarket> _markets = new Dictionary<BfProductCode, BfxMarket>();
        public BfxPositions Positions { get; } = new BfxPositions();

        public BfxAccount(string apiKey, string apiSecret)
        {
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiKey))
            {
                Client = new BitFlyerClient().AddTo(_disposables);
                RealtimeSource = new RealtimeSourceFactory(Client).AddTo(_disposables);
            }
            else
            {
                Client = new BitFlyerClient(apiKey, apiSecret).AddTo(_disposables);
                RealtimeSource = new RealtimeSourceFactory(apiKey, apiSecret, Client).AddTo(_disposables);
            }
            RealtimeSource.ConnectionResumed += OnRealtimeConnectionResumed;
        }

        public BfxAccount() : this(string.Empty, string.Empty)
        {
        }

        private void OnRealtimeConnectionResumed()
        {
            // ポジション情報の再読み込み後、遅延したイベントを受信しないのか？
            Positions.Update(Client.GetPositions(BfProductCode.FXBTCJPY).GetContent());
        }

        public void Dispose()
        {
            _disposables.DisposeReverse();
        }

        void InitializeMarkets()
        {
            if (_markets.Count > 0)
            {
                return;
            }

            Client.GetAvailableMarkets().ForEach(e =>
            {
                _markets.Add(e.ProductCode, new BfxMarket(this, e.ProductCode).AddTo(_disposables));
                _marketSymbols.Add(e.Symbol, e.ProductCode);
            });
        }

        public async Task OpenAsync()
        {
            InitializeMarkets();

            if (!Client.IsAuthenticated)
            {
                return;
            }

            Positions.Update(Client.GetPositions(BfProductCode.FXBTCJPY).GetContent());
            await RealtimeSource.TryOpenAsync();
            RealtimeSource.GetChildOrderEventsSource().Subscribe(coe =>
            {
                var productCode = _marketSymbols[coe.ProductCode];
                _markets[productCode].ForwardChildOrderEvents(coe);
                if (productCode == BfProductCode.FXBTCJPY && coe.EventType == BfOrderEventType.Execution)
                {
                    Positions.Update(coe).ForEach(e => PositionChanged?.Invoke(this, new BfxPositionEventArgs(coe.EventDate, e)));
                }
            });

            RealtimeSource.GetParentOrderEventsSource().Subscribe(poe =>
            {
                _markets[_marketSymbols[poe.ProductCode]].ForwardParentOrderEvents(poe);
            });
        }

        public BfxMarket GetMarket(BfProductCode productCode)
        {
            InitializeMarkets();
            return _markets[productCode];
        }
    }
}
