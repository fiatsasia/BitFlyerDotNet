//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Newtonsoft.Json;
using BitFlyerDotNet.LightningApi;

namespace RealtimeApiTests
{
    class Program
    {
        static char GetCh(bool echo = true) { var ch = Char.ToUpper(Console.ReadKey(true).KeyChar); if (echo) Console.WriteLine(ch); return ch; }
        static CompositeDisposable _disposables = new CompositeDisposable();
        static Dictionary<string, string> Properties;

        const BfProductCode ProductCode = BfProductCode.FXBTCJPY;
        static RealtimeSourceFactory _factory;
        static bool _detail;

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Console.Write("Disable authentication (y/n)?");
                if (GetCh() == 'Y')
                {
                    _factory = new RealtimeSourceFactory();
                }
                else
                {
                    LoadRunsettings(args[0]);
                    var key = Properties["ApiKey"];
                    var secret = Properties["ApiSecret"];
                    _factory = new RealtimeSourceFactory(key, secret);
                }
            }
            else
            {
                _factory = new RealtimeSourceFactory();
            }
            _factory.MessageSent += OnRealtimeMessageSent;
            _factory.MessageReceived += OnRealtimeMessageReceived;
            _factory.Error += (error) => Console.WriteLine("Error: {0} Socket Error = {1}", error.Message, error.SocketError);
            _factory.Open();

            while (true)
            {
                Console.WriteLine("========================================================================");
                Console.WriteLine("E)xecution               T)icker                 O)rderBook");
                Console.WriteLine("C)hild Order Events      P)arent Order Events    B)oard");
                Console.WriteLine();
                Console.WriteLine("D)etail                  S)top                   Q)uit");
                Console.WriteLine("========================================================================");

                switch (GetCh())
                {
                    case 'E':
                        _factory.GetExecutionSource(ProductCode).Subscribe(exec => { Console.Write("."); }).AddTo(_disposables);
                        break;

                    case 'T':
                        _factory.GetTickerSource(ProductCode).Subscribe(ticker => { Console.Write("."); }).AddTo(_disposables);
                        break;

                    case 'O':
                        _factory.GetOrderBookSource(ProductCode).Select(e => e.GetSnapshot(100)).Subscribe(ob => { Console.Write("."); }).AddTo(_disposables);
                        break;

                    case 'C': // To fire child-order-event, use bitFlyer Lightning browser operation
                        _factory.GetChildOrderEventsSource().Subscribe(order => { Console.Write("."); }).AddTo(_disposables);
                        break;

                    case 'P': // To fire parent-order-event, use bitFlyer Lightning browser operation
                        _factory.GetParentOrderEventsSource().Subscribe(order => { Console.Write("."); }).AddTo(_disposables);
                        break;

                    case 'B':
                        RealtimeOrderBookSample();
                        break;

                    case 'D':
                        _detail = !_detail;
                        break;

                    case 'S':
                        _disposables.Clear();
                        break;

                    case 'Q':
                        _disposables.Dispose();
                        return;
                }
            }
        }

        static void LoadRunsettings(string filePath)
        {
            var xml = XDocument.Load(filePath);
            var n = xml.Element("RunSettings").Elements("TestRunParameters");
            Properties = xml.Element("RunSettings").Element("TestRunParameters").Elements("Parameter").ToDictionary(e => e.Attribute("name").Value, e => e.Attribute("value").Value);
        }

        static void OnRealtimeMessageSent(string json)
        {
            Console.WriteLine($"Message sent: {json}");
        }

        static void OnRealtimeMessageReceived(string json, object message)
        {
            Console.Write("Message received: ");
            switch (message)
            {
                case BfaExecution[] execs:
                    Console.WriteLine($"BfExecution[{execs.Length}]:");
                    break;

                case BfTicker[] ticker:
                    break;

                case BfBoard board: // OrderBook
                    Console.WriteLine($"BfOrderBook Asks:{board.Asks.Length} Bids:{board.Bids.Length}:");
                    break;

                case BfChildOrderEvent[] coe:
                    Console.WriteLine($"BfChildOrderEvent[{coe.Length}]:");
                    break;

                case BfParentOrderEvent[] poe:
                    Console.WriteLine($"BfParentOrderEvent[{poe.Length}]:");
                    break;

                default:
                    Console.WriteLine($"{message.GetType().Name}:");
                    break;
            }
            if (!_detail)
            {
                return;
            }

            var jobject = JsonConvert.DeserializeObject(json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        // Somtimes stopped feed without any errors.
        // It should retry if it elapsed defined limit.
        static void RealtimeOrderBookSample()
        {
            var left = Console.CursorLeft;
            var top = Console.CursorTop;

            _factory.GetOrderBookSource(BfProductCode.FXBTCJPY)
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
    }

    static class RxUtil
    {
        public static TResult AddTo<TResult>(this TResult resource, CompositeDisposable disposable) where TResult : IDisposable
        {
            disposable.Add(resource);
            return resource;
        }
    }
}
