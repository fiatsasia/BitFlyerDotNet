//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Financial.Extensions;
using BitFlyerDotNet.LightningApi;

namespace RealtimeApiTests
{
    class Program
    {
        static CompositeDisposable _disposables = new CompositeDisposable();

        static void Main(string[] args)
        {
            var factory = new RealtimeSourceFactory();

            //SubscribeTickerSource(factory, BfProductCode.BTCUSD).AddTo(_disposables);
            //SubscribeExecutionSource(factory, BfProductCode.FXBTCJPY).AddTo(_disposables);
            SubscribeOrderbookSource(factory, BfProductCode.FXBTCJPY).AddTo(_disposables);
            Console.ReadLine();
            _disposables.Dispose();
        }

        static IDisposable SubscribeTickerSource(RealtimeSourceFactory factory, BfProductCode productCode)
        {
            var source = factory.GetTickerSource(productCode);
            return source.Subscribe(ticker =>
            {
                Console.WriteLine($"{ticker.Timestamp} P:{ticker.LastTradedPrice} A:{ticker.BestAsk} B:{ticker.BestBid}");
            });

        }

        static IDisposable SubscribeExecutionSource(RealtimeSourceFactory factory, BfProductCode productCode)
        {
            var source = factory.GetExecutionSource(productCode);
            return source.Subscribe(exec =>
            {
                Console.WriteLine($"{exec.ExecutedTime} P:{exec.Price} A:{exec.Side} B:{exec.Size}");
            });

        }

        static IDisposable SubscribeOrderbookSource(RealtimeSourceFactory factory, BfProductCode productCode)
        {
            var source = factory.GetOrderBookSource(productCode);
            return source.Select(orderBook => orderBook.GetSnapshot(15)).Subscribe(obs =>
            {
                foreach (var ask in obs.Asks.Reverse())
                {
                    Console.WriteLine($"Ask: P:{ask.Price} S:{ask.Size}");
                }
                Console.WriteLine($"Mid: P:{obs.MidPrice}");
                foreach (var bid in obs.Bids.Reverse())
                {
                    Console.WriteLine($"Bid: P:{bid.Price} S:{bid.Size}");
                }
            });
        }
    }
}
