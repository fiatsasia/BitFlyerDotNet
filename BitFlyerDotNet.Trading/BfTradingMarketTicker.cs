//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Financial.Extensions;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfTradingMarketTicker
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

        public BfMarketHealth Health { get; private set; }
        public BfHealthState MarketStatus => Health.Status;

        public double SFDDifference { get; private set; }
        public double SFDRate { get; private set; }

        public BfTradingMarketTicker(BfOrderBook orderBook, BfTicker nativeTicker, BfMarketHealth health, TimeSpan serverTimeDiff)
        {
            OrderBook = orderBook;
            NativeTicker = nativeTicker;
            Health = health;
            ServerTimeDiff = serverTimeDiff;
            UpdatedTime = DateTime.UtcNow + serverTimeDiff;
        }

        public BfTradingMarketTicker(BfOrderBook orderBook, BfTicker fxbtcTicker, BfTicker btcTicker, BfMarketHealth health, TimeSpan serverTimeDiff)
        {
            OrderBook = orderBook;
            NativeTicker = fxbtcTicker;
            Health = health;
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

    public class BfTradingMarketTickerSource : IObservable<BfTradingMarketTicker>
    {
        CompositeDisposable _disposables = new CompositeDisposable();
        IObservable<BfTradingMarketTicker> _source;

        DateTime _lastServerTime = DateTime.MinValue;
        TimeSpan _serverTimeDiff = TimeSpan.Zero;

        public BfTradingMarketTickerSource(BfTradingMarket market)
        {
            // If market is FX_BTC_JPY, get BTC_JPT ticker to calculate realtime SFD rate.
            if (market.ProductCode == BfProductCode.FXBTCJPY)
            {
                _source = Observable.Create<BfTradingMarketTicker>(observer =>
                {
                    // Merge order book, native ticker and market health(REST)
                    market.GetOrderBookSource().CombineLatest
                    (
                        market.RealtimeSource.GetTickerSource(BfProductCode.FXBTCJPY),
                        market.RealtimeSource.GetTickerSource(BfProductCode.BTCJPY),
                        Observable.Timer(TimeSpan.Zero, market.Config.MarketStatusConfirmInterval)
                            .Select(count => market.Client.GetMarketHealth(BfProductCode.FXBTCJPY).GetResult()),
                        (ob, fxbtcjpy, btcjpy, health) =>
                        {
                            if (fxbtcjpy.Timestamp > _lastServerTime)
                            {
                                _lastServerTime = fxbtcjpy.Timestamp;
                                _serverTimeDiff = _lastServerTime - DateTime.UtcNow;
                            }
                            return new BfTradingMarketTicker(ob, fxbtcjpy, btcjpy, health, _serverTimeDiff);
                        }
                    )
                    .Subscribe(observer).AddTo(_disposables);

                    return _disposables.Dispose;
                });
            }
            else
            {
                _source = Observable.Create<BfTradingMarketTicker>(observer =>
                {
                    // Merge order book, native ticker and market health(REST)
                    market.GetOrderBookSource().CombineLatest
                    (
                        market.RealtimeSource.GetTickerSource(market.ProductCode),
                        Observable.Timer(TimeSpan.Zero, market.Config.MarketStatusConfirmInterval)
                            .Select(count => market.Client.GetMarketHealth(market.ProductCode).GetResult()),
                        (ob, nt, health) =>
                        {
                            if (nt.Timestamp > _lastServerTime)
                            {
                                _lastServerTime = nt.Timestamp;
                                _serverTimeDiff = _lastServerTime - DateTime.UtcNow;
                            }
                            return new BfTradingMarketTicker(ob, nt, health, _serverTimeDiff);
                        }
                    )
                    .Subscribe(observer).AddTo(_disposables);

                    return _disposables.Dispose;
                });
            }
        }

        public IDisposable Subscribe(IObserver<BfTradingMarketTicker> observer)
        {
            return _source.Subscribe(observer);
        }
    }
}
