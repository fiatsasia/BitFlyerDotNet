//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Reactive.Linq;
using BitFlyerDotNet.LightningApi;
using BitFlyerDotNet.Trading;
using System.Text;

namespace TradingApiTests
{
    partial class Program
    {
        const BfProductCode ProductCode = BfProductCode.FXBTCJPY;
        const decimal UnexecutableGap = 50000m;
        const string TimeFormat = "yyyy/MM/dd HH:mm:ss.ffff";

        static char GetCh(bool echo = true) { var ch = Char.ToUpper(Console.ReadKey(true).KeyChar); if (echo) Console.WriteLine(ch); return ch; }
        const char ESCAPE = (char)0x1b;

        static BfxAccount _account;
        static BfxMarket _market;
        static Dictionary<string, string> Properties;
        static Queue<IBfxOrderTransaction> _transactions = new Queue<IBfxOrderTransaction>();
        static decimal _orderSize;

        static void Main(string[] args)
        {
            LoadSettings(args[0]);

            using (_account = new BfxAccount(Properties["ApiKey"], Properties["ApiSecret"]))
            using (_market = _account.GetMarket(ProductCode))
            {
                _account.PositionChanged += OnPositionChanged;
                _orderSize = _market.MinimumOrderSize;

                // Call event handler on another thread
                //market.OrderTransactionEvent += OnOrderTransactionEvent;
                Observable.FromEventPattern<BfxOrderTransactionEventArgs>(_market, nameof(_market.OrderTransactionEvent))
                .ObserveOn(System.Reactive.Concurrency.Scheduler.Default)
                .Subscribe(e =>
                {
                    OnOrderTransactionEvent(e.Sender, e.EventArgs);
                });

                _market.Open();
                while (true)
                {
                    Console.WriteLine("===================================================================");
                    Console.WriteLine("S)imple orders");
                    Console.WriteLine("C)onditional orders");
                    Console.WriteLine("");
                    Console.Write("Main>");

                    try
                    {
                        switch (GetCh())
                        {
                            case 'S':
                                SimpleOrders();
                                break;

                            case 'C':
                                ConditionalOrders();
                                break;

                            case ESCAPE:
                                return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        static void LoadSettings(string filePath)
        {
            var xml = XDocument.Load(filePath);
            var n = xml.Element("RunSettings").Elements("TestRunParameters");
            Properties = xml.Element("RunSettings").Element("TestRunParameters").Elements("Parameter").ToDictionary(e => e.Attribute("name").Value, e => e.Attribute("value").Value);
        }

        static BfTradeSide SelectSide()
        {
            Console.Write("B)uy S)ell : ");
            while (true)
            {
                switch (GetCh())
                {
                    case 'B':
                        return BfTradeSide.Buy;

                    case 'S':
                        return BfTradeSide.Sell;

                    case ESCAPE:
                        return BfTradeSide.Unknown;
                }
            }
        }

        static void OnPositionChanged(object sender, BfxPositionChangedEventArgs ev)
        {
            var pos = ev.Position;
            if (ev.IsOpened)
            {
                Console.WriteLine($"{pos.Open.ToString(TimeFormat)} 建玉発生　　 {pos.Side} P:{pos.OpenPrice} S:{pos.Size} TS:{_account.Positions.TotalSize}");
            }
            else // Closed
            {
                Console.WriteLine($"{pos.Close.Value.ToString(TimeFormat)} 建玉決済　　 {pos.Side} P:{pos.ClosePrice} S:{pos.Size} TS:{_account.Positions.TotalSize} PT:{pos.Profit} NP:{pos.NetProfit}");
            }
        }

        private static void OnOrderTransactionEvent(object sender, BfxOrderTransactionEventArgs ev)
        {
            var sb = new List<string>();
            sb.Add(ev.Time.ToString(TimeFormat));

            IBfxOrder order;
            if (ev.EventType != BfxOrderTransactionEventType.ChildOrderEvent)
            {
                order = ev.Order;
                sb.Add(ev.EventType.ToDisplayString());
            }
            else
            {
                order = ev.Order.Children[ev.ChildOrderIndex];
                sb.Add(ev.ChildEventType.ToChildDisplayString());
            }

            sb.Add($"{order.ProductCode}");
            sb.Add($"{order.OrderType}");
            if (order.Side.HasValue)
            {
                sb.Add($"{order.Side}");
            }
            if (order.OrderPrice.HasValue)
            {
                sb.Add($"P:{order.OrderPrice}");
            }
            if (order.OrderSize.HasValue)
            {
                sb.Add($"S:{order.OrderSize}");
            }
            if (order.ExecutedPrice.HasValue)
            {
                sb.Add($"EP:{order.ExecutedPrice}");
            }
            if (order.ExecutedSize.HasValue)
            {
                sb.Add($"ES:{order.ExecutedSize}");
            }
            Console.WriteLine(string.Join(' ', sb));

            switch (ev.EventType)
            {
                case BfxOrderTransactionEventType.Completed:
                    _transactions.Dequeue();
                    break;
            }
        }
    }
}
