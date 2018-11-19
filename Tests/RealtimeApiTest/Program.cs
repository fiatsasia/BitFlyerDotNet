//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using BitFlyerDotNet.LightningApi;

namespace RealtimeApiTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new BitFlyerRealtimeSourceFactory();
#if false
            factory.GetExecutionSource(BfProductCode.FXBTCJPY).Subscribe(tick =>
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5}",
                    tick.ExecutionId,
                    tick.Side,
                    tick.Price,
                    tick.Size,
                    tick.ExecutedTime.ToLocalTime(),
                    tick.ChildOrderAcceptanceId);
            });

            factory.StartAllExecutionSources();
#endif
#if true
            factory.GetTickerSource(BfProductCode.FXBTCJPY).Subscribe(ticker =>
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}",
                    ticker.ProductCode,
                    ticker.Timestamp.ToLocalTime(),
                    ticker.TickId,
                    ticker.BestBid,
                    ticker.BestAsk,
                    ticker.BestBidSize,
                    ticker.BestAskSize,
                    ticker.TotalBidDepth,
                    ticker.TotalAskDepth,
                    ticker.LastTradedPrice,
                    ticker.Last24HoursVolume,
                    ticker.VolumeByProduct);
            });
#endif
            Console.ReadLine();
        }
    }
}
