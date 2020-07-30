//==============================================================================
// Copyright 2017-2020 (C) By Fiats Inc.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Xml.Linq;
using System.Diagnostics;
using Newtonsoft.Json;
using BitFlyerDotNet.LightningApi;

namespace OrderApiSample
{
    partial class Program
    {
        static char GetCh(bool echo = true) { var ch = Char.ToUpper(Console.ReadKey(true).KeyChar); if (echo) Console.WriteLine(ch); return ch; }
        static CompositeDisposable _disposables = new CompositeDisposable();
        static ConsoleTraceListener _listener;

        const BfProductCode ProductCode = BfProductCode.FXBTCJPY;
        static BitFlyerClient _client;
        static RealtimeSourceFactory _factory;
        static BfTicker _ticker;

        static decimal _orderSize = ProductCode.MinimumOrderSize();
        static int _minuteToExpire = 1;

        // Pass key and secret from arguments.
        static void Main(string[] args)
        {
            _listener = new ConsoleTraceListener();

            // Set API ket and secret
            if (args.Length > 0)
            {
                LoadRunsettings(args[0]);
                var key = Properties["ApiKey"];
                var secret = Properties["ApiSecret"];
                _client = new BitFlyerClient(key, secret);
                _factory = new RealtimeSourceFactory(key, secret);
            }
            else
            {
                Console.Write("API Key   :"); var key = Console.ReadLine();
                Console.Write("API Secret:"); var secret = Console.ReadLine();
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(key))
                {
                    Console.WriteLine("API key and secret are required.");
                    Console.ReadLine();
                    return;
                }
                _client = new BitFlyerClient(key, secret);
                _factory = new RealtimeSourceFactory(key, secret);
            }
            _factory.MessageReceived += OnRealtimeMessageReceived;

            _disposables.Add(_client);
            _disposables.Add(_factory);

            _client.ConfirmCallback = ConfirmOrder;

            _disposables.Add(_factory.GetTickerSource(ProductCode).Subscribe(OnTickerChanged));
            _disposables.Add(_factory.GetChildOrderEventsSource().Subscribe(OnChildOrderEvent));
            _disposables.Add(_factory.GetParentOrderEventsSource().Subscribe(OnParentOrderEvent));

            while (true)
            {
                Console.WriteLine("======== Main menu");
                Console.WriteLine("C)hild order samples");
                Console.WriteLine("P)arent order samples");
                Console.WriteLine("E)nable debug dump");
                Console.WriteLine("D)isable debug dump");
                Console.WriteLine("");
                Console.WriteLine("Q) Quit sample");

                switch (GetCh())
                {
                    case 'C':
                        ChildOrderMain();
                        break;

                    case 'P':
                        ParentOrderMain();
                        break;

                    case 'E':
                        Trace.Listeners.Add(_listener); // Debug.Trace output to console
                        break;

                    case 'D':
                        Trace.Listeners.Remove(_listener);
                        break;

                    case 'Q':
                        _disposables.Dispose();
                        return;
                }
            }
        }

        static Dictionary<string, string> Properties;
        static void LoadRunsettings(string filePath)
        {
            var xml = XDocument.Load(filePath);
            Properties = xml.Element("RunSettings").Element("TestRunParameters").Elements("Parameter").ToDictionary(e => e.Attribute("name").Value, e => e.Attribute("value").Value);
        }

        static bool ConfirmOrder(string apiName, string json)
        {
            Console.WriteLine($"{apiName}:");
            var jobject = JsonConvert.DeserializeObject(json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));

            return false;
        }

        static void OnRealtimeMessageReceived(string json, object message)
        {
            switch (message)
            {
                case BfTicker _:
                    return;

                default:
                    break;
            }

            Console.WriteLine($"{message.GetType().Name}:");
            var jobject = JsonConvert.DeserializeObject(json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        // Trading ticker is streaming from order book source and support SFD rate if market is FX_BTC_JPY
        static void OnTickerChanged(BfTicker ticker)
        {
            _ticker = ticker;
        }

        static void OnChildOrderEvent(BfChildOrderEvent evt)
        {
        }

        static void OnParentOrderEvent(BfParentOrderEvent evt)
        {
        }
    }
}
