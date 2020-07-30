//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using BitFlyerDotNet.LightningApi;
using System.Reactive.Disposables;

namespace BitFlyerDotNet.Trading
{
    public partial class BfxAccount : IDisposable
    {
        public BitFlyerClient Client { get; private set; }
        public RealtimeSourceFactory RealtimeSource { get; private set; }

        CompositeDisposable _disposables = new CompositeDisposable();
        Dictionary<string, BfProductCode> _marketSymbols = new Dictionary<string, BfProductCode>();
        Dictionary<BfProductCode, BfxMarket> _markets = new Dictionary<BfProductCode, BfxMarket>();

        public BfxAccount(string apiKey, string apiSecret)
        {
            Client = new BitFlyerClient(apiKey, apiSecret).AddTo(_disposables);
            RealtimeSource = new RealtimeSourceFactory(apiKey, apiSecret).AddTo(_disposables);
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

            RealtimeSource.GetChildOrderEventsSource().Subscribe(coe =>
            {
                _markets[_marketSymbols[coe.ProductCode]].RedirectChildOrderEvents(coe);
            });

            RealtimeSource.GetParentOrderEventsSource().Subscribe(poe =>
            {
                _markets[_marketSymbols[poe.ProductCode]].RedirectParentOrderEvents(poe);
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
