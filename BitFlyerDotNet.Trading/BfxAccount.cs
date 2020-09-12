//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Generic;
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
            Client = new BitFlyerClient(apiKey, apiSecret).AddTo(_disposables);
            RealtimeSource = new RealtimeSourceFactory(apiKey, apiSecret, Client).AddTo(_disposables);
            RealtimeSource.ConnectionResumed += OnRealtimeConnectionResumed;
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

        public void Open()
        {
            Client.GetAvailableMarkets().ForEach(e =>
            {
                _markets.Add(e.ProductCode, new BfxMarket(this, e.ProductCode).AddTo(_disposables));
                _marketSymbols.Add(e.Symbol, e.ProductCode);
            });

            Positions.Update(Client.GetPositions(BfProductCode.FXBTCJPY).GetContent());

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

        void TryOpen()
        {
            if (_markets.Count == 0)
            {
                Open();
            }
        }

        public BfxMarket GetMarket(BfProductCode productCode)
        {
            TryOpen();
            var market = _markets[productCode];
            market.TryOpen();
            return market;
        }
    }
}
