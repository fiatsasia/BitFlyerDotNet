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
        static char GetCh(bool echo = true) { var ch = Char.ToUpper(Console.ReadKey(true).KeyChar); if (echo) Console.WriteLine(ch); return ch; }

        static Queue<IBfxOrderTransaction> _transactions = new Queue<IBfxOrderTransaction>();

        static void Main(string[] args)
        {
            LoadRunsettings(args[0]);

            using (var account = new BfxAccount(Properties["ApiKey"], Properties["ApiSecret"]))
            using (var market = account.GetMarket(ProductCode))
            {
                while (true)
                {
                    Console.WriteLine("========================================================================");
                    Console.WriteLine("1) Buy best-bid price");
                    Console.WriteLine("2) Sell best-ask price");
                    Console.WriteLine("");
                    Console.WriteLine("Q)uit");
                    Console.WriteLine("========================================================================");

                    switch (GetCh())
                    {
                        case '1':
                            {
                                var order = BfxSimpleOrder.LimitPrice(market, BfTradeSide.Buy, market.Ticker.BestBidPrice, market.MinimumOrderSize);
                                var tran = market.PlaceOrder(order);
                                _transactions.Enqueue(tran);
                            }
                            break;

                        case '2':
                            {
                                var order = BfxSimpleOrder.LimitPrice(market, BfTradeSide.Sell, market.Ticker.BestAskPrice, market.MinimumOrderSize);
                                var tran = market.PlaceOrder(order);
                                _transactions.Enqueue(tran);
                            }
                            break;

                        case 'Q':
                            return;
                    }
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
    }
}
