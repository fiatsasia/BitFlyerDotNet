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
            var factory = new BitFlyerRealtimeSourceFactory(BfRealtimeSourceKind.WebSocket);
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
#if false
            factory.GetTickerSource(BfProductCode.BTCJPYMAT3M).Subscribe(tick =>
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}",
                    tick.ProductCode,
                    tick.Timestamp.ToLocalTime(),
                    tick.TickId,
                    tick.BestBid,
                    tick.BestAsk,
                    tick.BestBidSize,
                    tick.BestAskSize,
                    tick.TotalBidDepth,
                    tick.TotalAskDepth,
                    tick.LastTradedPrice,
                    tick.Last24HoursVolume,
                    tick.VolumeByProduct);
            });
#endif
            Console.ReadLine();
        }
    }
}
