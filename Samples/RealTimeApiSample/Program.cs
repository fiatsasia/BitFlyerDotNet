using System;
using BitFlyerDotNet.LightningApi;

namespace RealTimeApiSample
{
    class Program
    {
        static char GetCh(bool echo = true) { var ch = Char.ToUpper(Console.ReadKey(true).KeyChar); if (echo) Console.WriteLine(ch); return ch; }

        static void Main(string[] args)
        {
            var factory = new BitFlyerRealtimeSourceFactory();
            factory.ErrorHandlers += (error) =>
            {
                Console.WriteLine("Error: {0} Socket Error = {1}", error.Message, error.SocketError);
            };

            Console.WriteLine("1) RealtimeExecutionSample");
            Console.WriteLine("2) RealtimeTickerSample");

            switch (GetCh())
            {
                case '1':
                    RealtimeExecutionSample(factory);
                    break;

                case '2':
                    RealtimeTickerSample(factory);
                    break;
            }

            Console.ReadLine();
        }

        static void RealtimeExecutionSample(BitFlyerRealtimeSourceFactory factory)
        {
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
        }

        static void RealtimeTickerSample(BitFlyerRealtimeSourceFactory factory)
        {
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
        }
    }
}
