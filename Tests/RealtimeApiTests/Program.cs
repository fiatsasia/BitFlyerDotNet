//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using BitFlyerDotNet.LightningApi;

namespace RealtimeApiTests
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new RealtimeSourceFactory();
            //var disp = SubscribeTickerSource(factory, BfProductCode.BTCUSD);
            var disp = SubscribeExecutionSource(factory, BfProductCode.BTCUSD);
            Console.ReadLine();
            disp.Dispose();
            Console.ReadLine();
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
    }
}
