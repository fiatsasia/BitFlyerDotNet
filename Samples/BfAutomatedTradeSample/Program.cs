//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive.Linq;
using BitFlyerDotNet.LightningApi;

namespace BfAutomatedTradeSample
{
    // This sample application is:
    // Exchange : bitFlyer
    // Product  : FX_BTC_JPY
    // Method   : Simple moving average(SMA) crossover 5/15 minutes
    class Program
    {
        // Trading parameters
        const BfProductCode ProductCode = BfProductCode.FXBTCJPY;
        static readonly TimeSpan TickPeriod = TimeSpan.FromMinutes(1);
        const int ShortPeriod = 5; // 5 minutes
        const int LongPeriod = 15; // 15 minutes

        static BitFlyerClient _client;

        static void Main(string[] args)
        {
            // Create bitFlyer API client as security mode
            Console.WriteLine("If you do not want real trade, skip key and secret below.");
            Console.Write("Key:"); var key = Console.ReadLine();
            Console.Write("Secret:"); var secret = Console.ReadLine();
            _client = new BitFlyerClient(key, secret);

            // Create FXBTCJPY minutely OHLC source
            var factory = new BitFlyerRealtimeSourceFactory(BfRealtimeSourceKind.PubNub);
            var ohlcSource = factory.GetExecutionSource(ProductCode).Buffer(TickPeriod).Select(ticks =>
            {
                var ohlc = new Ohlc(ticks);
                Console.WriteLine("{0} O:{1} H:{2} L:{3} C:{4} V:{5}",
                    ohlc.Start.ToLocalTime(),
                    ohlc.Open,
                    ohlc.High,
                    ohlc.Low,
                    ohlc.Close,
                    ohlc.Volume);
                return ohlc;
            });

            // Calculate simple Moving Average(SMA) with close price and combine OHLC/(5/15 SMA variance) for trading signal
            ohlcSource.Publish(s => s.WithLatestFrom(
                s.Select(ohlc => ohlc.Close).SimpleMovingAverage(ShortPeriod).WithLatestFrom(
                    s.Select(ohlc => ohlc.Close).SimpleMovingAverage(LongPeriod),
                    (spv, lpv) =>
                    {
                        return spv - lpv; // Calculate variance: SMA short period value - long period value
                    }
                ),
                (ohlc, variance) =>
                {
                    return new Tuple<Ohlc, double>(ohlc, variance); // Combine OHLC/calculated variance
                }
            ))
            .Where(value => Math.Sign(value.Item2) != 0) // Skip no variance tick
            .Buffer(2, 1) // convert to (prev, current)
            .Subscribe(OnTradeSignal);

            factory.StartExecutionSource(ProductCode); // Start execution source that will start feed realtime execution

            Console.ReadLine();
        }

        static void OnTradeSignal(IList<Tuple<Ohlc, double>> signals)
        {
            var ohlc = signals[1].Item1;
            var variance = signals[1].Item2;
            var prevVariance = signals[0].Item2;
            var tradeSignal = BfTradeSide.Unknown;

            if (Math.Sign(prevVariance) != Math.Sign(variance))
            {
                switch (Math.Sign(variance))
                {
                    case 1:
                        tradeSignal = BfTradeSide.Buy;
                        break;

                    case -1:
                        tradeSignal = BfTradeSide.Sell;
                        break;
                }
            }

            Console.WriteLine("{0} C:{1} VAR:{2} Singal:{3}",
                ohlc.Start.ToLocalTime(),
                ohlc.Close,
                variance,
                tradeSignal
            );

            switch (tradeSignal)
            {
                // Short period exceeded long
                case BfTradeSide.Buy:
                    Console.WriteLine("{0} Short period exceeded long. Buy signaled.", ohlc.Start.ToLocalTime());
                    //_client.SendChildOrder(ProductCode, BfOrderType.Limit, tradeSignal, ohlc.Close, 0.001);
                    break;

                // Short period belowed long
                case BfTradeSide.Sell:
                    Console.WriteLine("{0} Short period belowed long. Sell signaled.", ohlc.Start.ToLocalTime());
                    //_client.SendChildOrder(ProductCode, BfOrderType.Limit, tradeSignal, ohlc.Close, 0.001);
                    break;

                default:
                    Console.WriteLine("{0} Short/Long SMA did not toggle. Signal skipped.", ohlc.Start.ToLocalTime());
                    break;
            }
        }
    }
}
