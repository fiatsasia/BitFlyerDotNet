﻿//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxTicker
    {
        public BfOrderBook OrderBook { get; private set; }
        public decimal BestBidPrice => OrderBook?.BestBidPrice ?? decimal.Zero;
        public decimal BestBidSize => OrderBook?.BestBidSize ?? decimal.Zero;
        public decimal BestAskPrice => OrderBook?.BestAskPrice ?? decimal.Zero;
        public decimal BestAskSize => OrderBook?.BestAskSize ?? decimal.Zero;

        public BfTicker NativeTicker { get; private set; }
        public decimal LastTradedPrice => NativeTicker.LastTradedPrice;
        public DateTime UpdatedTime { get; private set; }
        public TimeSpan ServerTimeDiff { get; private set; }

        public double SFDDifference { get; private set; }
        public double SFDRate { get; private set; }

        public BfxTicker(BfOrderBook orderBook, BfTicker nativeTicker, TimeSpan serverTimeDiff)
        {
            OrderBook = orderBook;
            NativeTicker = nativeTicker;
            ServerTimeDiff = serverTimeDiff;
            UpdatedTime = DateTime.UtcNow + serverTimeDiff;
        }

        public BfxTicker(BfOrderBook orderBook, BfTicker fxbtcTicker, BfTicker btcTicker, TimeSpan serverTimeDiff)
        {
            OrderBook = orderBook;
            NativeTicker = fxbtcTicker;
            ServerTimeDiff = serverTimeDiff;
            UpdatedTime = DateTime.UtcNow + serverTimeDiff;

            if (fxbtcTicker != null && btcTicker != null)
            {
                SFDDifference = unchecked((double)((fxbtcTicker.LastTradedPrice - btcTicker.LastTradedPrice) / btcTicker.LastTradedPrice));
                SFDRate = CalculateSfdRate(Math.Abs(SFDDifference));
            }
        }

        double CalculateSfdRate(double variance)
        {
            var sfd = 0.0;
            if (Math.Abs(variance) < 0.05)
            {
                return 0.0; // SFD is none
            }
            else if (Math.Abs(variance) < 0.1) // 5% <= variance < 10%
            {
                sfd = 0.0025;
            }
            else if (Math.Abs(variance) < 0.15) // 10% <= variance < 15%
            {
                sfd = 0.005;
            }
            else if (Math.Abs(variance) < 0.20) // 15% <= variance < 20%
            {
                sfd = 0.01;
            }
            else // variance >= 20%
            {
                sfd = 0.02;
            }

            return sfd;
        }
    }

    public class BfxTickerSource : IObservable<BfxTicker>
    {
        CompositeDisposable _disposables = new CompositeDisposable();
        IObservable<BfxTicker> _source;

        DateTime _lastServerTime = DateTime.MinValue;
        TimeSpan _serverTimeDiff = TimeSpan.Zero;

        public BfxTickerSource(BfxMarket market)
        {
            // If market is FX_BTC_JPY, get BTC_JPT ticker to calculate realtime SFD rate.
            if (market.ProductCode == BfProductCode.FXBTCJPY)
            {
                _source = Observable.Create<BfxTicker>(observer =>
                {
                    // Merge order book, native ticker and market health(REST)
                    market.RealtimeSource.GetOrderBookSource(market.ProductCode).CombineLatest
                    (
                        market.RealtimeSource.GetTickerSource(BfProductCode.FXBTCJPY),
                        market.RealtimeSource.GetTickerSource(BfProductCode.BTCJPY),
                        (ob, fxbtcjpy, btcjpy) =>
                        {
                            if (fxbtcjpy.Timestamp > _lastServerTime)
                            {
                                _lastServerTime = fxbtcjpy.Timestamp;
                                _serverTimeDiff = fxbtcjpy.Timestamp - DateTime.UtcNow;
                            }
                            return new BfxTicker(ob, fxbtcjpy, btcjpy, _serverTimeDiff);
                        }
                    )
                    .Subscribe(observer).AddTo(_disposables);

                    return _disposables.Dispose;
                });
            }
            else
            {
                _source = Observable.Create<BfxTicker>(observer =>
                {
                    // Merge order book, native ticker and market health(REST)
                    market.RealtimeSource.GetOrderBookSource(market.ProductCode).CombineLatest
                    (
                        market.RealtimeSource.GetTickerSource(market.ProductCode),
                        (ob, nt) =>
                        {
                            if (nt.Timestamp > _lastServerTime)
                            {
                                _lastServerTime = nt.Timestamp;
                                _serverTimeDiff = nt.Timestamp - DateTime.UtcNow;
                            }
                            return new BfxTicker(ob, nt, _serverTimeDiff);
                        }
                    )
                    .Subscribe(observer).AddTo(_disposables);

                    return _disposables.Dispose;
                });
            }
        }

        public IDisposable Subscribe(IObserver<BfxTicker> observer)
        {
            return _source.Subscribe(observer);
        }
    }
}
