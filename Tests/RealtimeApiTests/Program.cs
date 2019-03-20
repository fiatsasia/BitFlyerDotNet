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
            var source = factory.GetTickerSource(BfProductCode.BTCEUR);
            var disp = source.Subscribe(ticker =>
            {
                Console.WriteLine($"{ticker.Timestamp} P:{ticker.LastTradedPrice} A:{ticker.BestAsk} B:{ticker.BestBid}");
            });

            Console.ReadLine();
            disp.Dispose();
            Console.ReadLine();
        }
    }
}
