//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using BitFlyerDotNet.LightningApi;

namespace OrderApiTests
{
    partial class Program
    {
        static char GetCh(bool echo = true) { var ch = Char.ToUpper(Console.ReadKey(true).KeyChar); if (echo) Console.WriteLine(ch); return ch; }
        static CompositeDisposable _disposables = new CompositeDisposable();

        const string ProductCode = BfProductCode.FX_BTC_JPY;
        static readonly decimal OrderSize = 0.01m; // ProductCode.GetMinimumOrderSize();
        const decimal UnexecuteGap = 50000m;
        static BitFlyerClient _client;
        static RealtimeSourceFactory _factory;

        static BfTicker _ticker;

        static void Main(string[] args)
        {
            // ログファイル出力設定
            // Time / JSON

            LoadRunsettings(args[0]);
            var key = Properties["ApiKey"];
            var secret = Properties["ApiSecret"];

            _client = new BitFlyerClient(key, secret);

            _factory = new RealtimeSourceFactory(key, secret);
            _factory.Channel.MessageReceived += OnRealtimeMessageReceived;
            _factory.Error += (error) => Console.WriteLine("Error: {0} Socket Error = {1}", error.Message, error.SocketError);
            _factory.GetTickerSource(ProductCode).Subscribe(ticker =>
            {
                _ticker = ticker;
                //Console.WriteLine($"Ask:{_ticker.BestAsk} Bid:{_ticker.BestBid}");
            }).AddTo(_disposables);
            _factory.GetChildOrderEventsSource().Subscribe(OnChildOrderEvent).AddTo(_disposables);
            _factory.GetParentOrderEventsSource().Subscribe(OnParentOrderEvent).AddTo(_disposables);

            while (true)
            {
                Console.WriteLine("========================================================================");
                Console.WriteLine("C)hild order  >");
                Console.WriteLine("P)arent order >");
                Console.WriteLine("");
                Console.WriteLine("Q)uit");
                Console.WriteLine("========================================================================");

                switch (GetCh())
                {
                    case 'C':
                        ChildOrderMain();
                        break;

                    case 'P':
                        ParentOrderMain();
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
            var n = xml.Element("RunSettings").Elements("TestRunParameters");
            Properties = xml.Element("RunSettings").Element("TestRunParameters").Elements("Parameter").ToDictionary(e => e.Attribute("name").Value, e => e.Attribute("value").Value);
        }

        static void OnRealtimeMessageReceived(object message)
        {
            switch (message)
            {
                case BfChildOrderEvent[] coe:
                    Console.WriteLine($"BfChildOrderEvent[{coe.Length}]:");
                    break;

                case BfParentOrderEvent[] poe:
                    Console.WriteLine($"BfParentOrderEvent[{poe.Length}]:");
                    break;

                default:
                    return;
            }
        }

        static void OnChildOrderEvent(BfChildOrderEvent coe)
        {
            switch (coe.EventType)
            {
            }
        }

        static void OnParentOrderEvent(BfParentOrderEvent poe)
        {
            switch (poe.EventType)
            {
            }
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
