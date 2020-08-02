//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using BitFlyerDotNet.LightningApi;
using BitFlyerDotNet.Trading;

namespace TradingApiTests
{
    class Program
    {
        const BfProductCode ProductCode = BfProductCode.FXBTCJPY;
        const decimal UnexecuteGap = 50000m;
        const string TimeFormat = "yyyy/MM/dd HH:mm:ss.ffff";

        static char GetCh(bool echo = true) { var ch = Char.ToUpper(Console.ReadKey(true).KeyChar); if (echo) Console.WriteLine(ch); return ch; }

        static Dictionary<string, string> Properties;
        static Queue<IBfxOrderTransaction> _transactions = new Queue<IBfxOrderTransaction>();

        static void Main(string[] args)
        {
            LoadSettings(args[0]);

            using (var account = new BfxAccount(Properties["ApiKey"], Properties["ApiSecret"]))
            using (var market = account.GetMarket(ProductCode))
            {
                account.PositionChanged += OnPositionChanged;
                market.OrderTransactionEvent += OnOrderTransactionEvent;
                market.Open();
                while (true)
                {
                    Console.WriteLine("========================================================================");
                    Console.WriteLine("B)uy best-bid price");
                    Console.WriteLine("S)ell best-ask price");
                    Console.WriteLine("3) Stop sell unexecutable price");
                    Console.WriteLine("4) IFD unexecutable price");
                    Console.WriteLine("C)ancel last order");
                    Console.WriteLine("6) Limit sell unexecutable price");
                    Console.WriteLine("");
                    Console.WriteLine("Q)uit");
                    Console.WriteLine("========================================================================");

                    switch (GetCh())
                    {
                        case 'B':
                            _transactions.Enqueue(market.PlaceOrder(BfxOrder.LimitPrice(BfTradeSide.Buy, market.Ticker.BestBidPrice, market.MinimumOrderSize)));
                            break;

                        case 'S':
                            _transactions.Enqueue(market.PlaceOrder(BfxOrder.LimitPrice(BfTradeSide.Sell, market.Ticker.BestAskPrice, market.MinimumOrderSize)));
                            break;

                        case '3':
                            _transactions.Enqueue(market.PlaceOrder(BfxOrder.StopLimit(
                                BfTradeSide.Sell,
                                market.Ticker.BestAskPrice + UnexecuteGap,
                                market.Ticker.BestAskPrice + UnexecuteGap,
                                market.MinimumOrderSize
                            )));
                            break;

                        case '4':
                            _transactions.Enqueue(market.PlaceOrder(BfxOrder.IFD(
                                BfxOrder.LimitPrice(
                                    BfTradeSide.Sell,
                                    market.Ticker.BestAskPrice + UnexecuteGap,
                                    market.MinimumOrderSize
                                ),
                                BfxOrder.StopLimit(
                                    BfTradeSide.Sell,
                                    market.Ticker.BestAskPrice + UnexecuteGap,
                                    market.Ticker.BestAskPrice + UnexecuteGap,
                                    market.MinimumOrderSize
                                )
                            )));
                            break;

                        case 'C':
                            _transactions.Dequeue().CancelOrder();
                            break;

                        case '6':
                            _transactions.Enqueue(market.PlaceOrder(BfxOrder.LimitPrice(BfTradeSide.Sell, market.Ticker.BestAskPrice + UnexecuteGap, market.MinimumOrderSize)));
                            break;

                        case 'Q':
                            return;
                    }
                }
            }
        }

        static void OnPositionChanged(object sender, BfxPositionChangedEventArgs e)
        {
            var pos = e.Position;
            if (e.IsOpened)
            {
                Console.WriteLine($"{pos.Open} Position Opened");
            }
            else // Closed
            {
                Console.WriteLine($"{pos.Close} Position Closed : P:{pos.Profit} NP:{pos.NetProfit}");
            }
        }

        static void LoadSettings(string filePath)
        {
            var xml = XDocument.Load(filePath);
            var n = xml.Element("RunSettings").Elements("TestRunParameters");
            Properties = xml.Element("RunSettings").Element("TestRunParameters").Elements("Parameter").ToDictionary(e => e.Attribute("name").Value, e => e.Attribute("value").Value);
        }

        private static void OnOrderTransactionEvent(object sender, BfxOrderTransactionEventArgs evt)
        {
            var time = evt.Time.ToString(TimeFormat);
            var eventMessage = evt.EventType.ToDisplayString();
            var orderMessageElements = new List<string>
            {
                $"{evt.Order.ProductCode}",
                $"{evt.Order.OrderType}",
            };
            if (evt.Order.Side.HasValue)
            {
                orderMessageElements.Add($"{evt.Order.Side}");
            }
            if (evt.Order.OrderPrice.HasValue)
            {
                orderMessageElements.Add($"Price:{evt.Order.OrderPrice}");
            }
            if (evt.Order.OrderSize.HasValue)
            {
                orderMessageElements.Add($"Size:{evt.Order.OrderSize}");
            }
            var orderMessage = string.Join(' ', orderMessageElements);

            //Console.WriteLine($"{time} Event:{evt.EventType} Tran:{evt.State} Order:{evt.OrderState}");
            Console.WriteLine($"{time} {eventMessage} {orderMessage}");
        }
    }
}
