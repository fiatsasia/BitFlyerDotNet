//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Financial.Extensions;
using BitFlyerDotNet.LightningApi;

namespace RealtimeApiSample
{
    class Program
    {
        static char GetCh(bool echo = true) { var ch = Char.ToUpper(Console.ReadKey(true).KeyChar); if (echo) Console.WriteLine(ch); return ch; }
        static CompositeDisposable _disposables = new CompositeDisposable();

        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            if (args.Length > 0)
            {
                LoadRunsettings(args[0]);
            }

            var factory = new RealtimeSourceFactory();
            factory.Error += (error) =>
            {
                Console.WriteLine("Error: {0} Socket Error = {1}", error.Message, error.SocketError);
            };

            Console.WriteLine("1) RealtimeExecution sample");
            Console.WriteLine("2) RealtimeTicker sample");
            Console.WriteLine("3) RealtimeOrderBook sample");
            Console.WriteLine("4) RealtimeChildOrderEvents sample");
            Console.WriteLine("5) RealtimeParentOrderEvents sample");

            switch (GetCh())
            {
                case '1':
                    RealtimeExecutionSample(factory);
                    break;

                case '2':
                    RealtimeTickerSample(factory);
                    break;

                case '3':
                    RealtimeOrderBookSample(factory);
                    break;

                case '4':
                    RealtimeChildOrderEvents(factory);
                    break;

                case '5':
                    RealtimeParentOrderEvents(factory);
                    break;
            }

            Console.ReadLine();
            _disposables.Dispose();
        }

        static Dictionary<string, string> Properties;
        static void LoadRunsettings(string filePath)
        {
            var xml = XDocument.Load(filePath);
            var n = xml.Element("RunSettings").Elements("TestRunParameters");
            Properties = xml.Element("RunSettings").Element("TestRunParameters").Elements("Parameter").ToDictionary(e => e.Attribute("name").Value, e => e.Attribute("value").Value);
        }

        static void RealtimeExecutionSample(RealtimeSourceFactory factory)
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
            }).AddTo(_disposables);
        }

        static void RealtimeTickerSample(RealtimeSourceFactory factory)
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
            }).AddTo(_disposables);
        }

        // Somtimes stopped feed without any errors.
        // It should retry if it elapsed defined limit.
        static void RealtimeOrderBookSample(RealtimeSourceFactory factory)
        {
            var left = Console.CursorLeft;
            var top = Console.CursorTop;

            factory.GetOrderBookSource(BfProductCode.FXBTCJPY)
            .Select(orderBook => orderBook.GetSnapshot(15)) // Take 15 orders from 300 orders
            .Subscribe(obs =>
            {
                Console.SetCursorPosition(left, top);
                foreach (var ask in obs.Asks.Reverse())
                {
                    Console.WriteLine($"{ask.Size.ToString("##0.00000000#")} {ask.Price}           ");
                }
                Console.WriteLine($"Mid:       {obs.MidPrice}");
                foreach (var bid in obs.Bids.Reverse())
                {
                    Console.WriteLine($"           {bid.Price} {bid.Size.ToString("##0.00000000#")}");
                }
            }).AddTo(_disposables);
        }

        static void RealtimeChildOrderEvents(RealtimeSourceFactory factory)
        {
            var key = Properties["ApiKey"];
            var secret = Properties["ApiSecret"];

            factory.GetChildOrderEventsSource(key, secret).Subscribe(order =>
            {
                Console.WriteLine($"{order.ProductCode} {order.ChildOrderId} {order.EventType}");
            }).AddTo(_disposables);
        }

        static void RealtimeParentOrderEvents(RealtimeSourceFactory factory)
        {
        }
    }
}
