//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Newtonsoft.Json;
using BitFlyerDotNet.LightningApi;
using BitFlyerDotNet.Trading;

namespace TradingApiTests
{
    partial class Program
    {
        const string ProductCode = "FX_BTC_JPY";
        const decimal UnexecutableGap = 50000m;
        const string TimeFormat = "yyyy/MM/dd HH:mm:ss.ffff";
        const string OrderCacheFileName = "TradingApiTests.db3";
        const string TabString = "    ";

        static char GetCh(bool echo = true) { var ch = Char.ToUpper(Console.ReadKey(true).KeyChar); if (echo) Console.WriteLine(ch); return ch; }
        const char ESCAPE = (char)0x1b;

        static Dictionary<string, string> Properties;
        static decimal _orderSize;

        static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
            LoadSettings(args[0]);
            var key = Properties["ApiKey"];
            var secret = Properties["ApiSecret"];

            Console.Write("Disable authentication (y/n)?");
            if (GetCh() == 'Y')
            {
                key = secret = string.Empty;
            }

            //_orderSize = ProductCode.GetMinimumOrderSize();
            _orderSize = 0.01m;

            var connStr = "data source=" + Path.Combine(Properties["CacheDirectoryPath"], OrderCacheFileName);
            using (var app = new BfxApplication(key, secret))
            {
                app.OrderChanged += OnOrderChanged;
                app.PositionChanged += OnPositionChanged;

                while (true)
                {
                    Console.WriteLine("===================================================================");
                    Console.WriteLine("S)imple orders");
                    Console.WriteLine("C)onditional orders");
                    Console.WriteLine("U)nexecutable orders");
                    Console.WriteLine("");
                    Console.WriteLine("Active O)rders");
                    Console.WriteLine("T)oday's Profit");
                    Console.WriteLine("Active P)ositions");
                    Console.WriteLine("");
                    Console.Write("Main>");

                    try
                    {
                        switch (GetCh())
                        {
                            case 'S':
                                await SimpleOrders(app);
                                break;

                            case 'C':
                                await ConditionalOrders(app);
                                break;

                            case 'U':
                                await UnexecutableOrders(app);
                                break;

                            case 'P':
                                GetActivePositions();
                                break;

                            case 'O':
                                var orders = await app.GetActiveOrdersAsync(ProductCode);
                                foreach (var order in orders)
                                {
                                    PrintOrder(order);
                                }
                                break;

                            case 'T':
                                //Console.WriteLine($"Profit {_orderCache.CalculateProfit(TimeSpan.FromDays(1))} JPY");
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

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            Console.WriteLine("Press Enter to continue");
            Console.ReadLine();
            Environment.Exit(1);
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

        static void CancelOrder()
        {
            /*var tran = _transactions.Values.Where(e => e.IsCancelable).OrderBy(e => e.OpenTime).FirstOrDefault();
            if (tran != null)
            {
                tran.Cancel();
            }*/
        }

        static async void ClosePositions(BfxApplication app)
        {
            /*if (_account.Positions.TotalSize > 0m)
            {
                await app.PlaceOrderAsync(BfChildOrderRequest.Market(ProductCode, _account.Positions.Side.GetOpposite(), _account.Positions.TotalSize));
            }*/
        }

        static void GetActivePositions()
        {
            /*_account.Positions.GetActivePositions().ForEach(e => PrintPosition(e));
            DumpResponse(_account.Client.GetPositions(ProductCode));*/
        }

        static void OnPositionChanged(object sender, BfxPositionChangedEventArgs ev)
        {
            PrintPosition(ev.Position);
        }

        private static void OnOrderChanged(object sender, BfxOrderChangedEventArgs e)
        {
            var sb = new List<string>();
            sb.Add(e.Time.ToString(TimeFormat));

            if (e.EventType != BfxOrderEventType.ChildOrderEvent)
            {
                sb.Add(e.EventType.ToString());
            }
            else
            {
                sb.Add(e.ChildEventType.ToString());
            }

            sb.Add($"{e.Order.ProductCode}");
            sb.Add($"{e.Order.OrderType}");
            if (e.Order.Side.HasValue)
            {
                sb.Add($"{e.Order.Side}");
            }
            if (e.Order.OrderPrice.HasValue)
            {
                sb.Add($"P:{e.Order.OrderPrice}");
            }
            if (e.Order.OrderSize.HasValue)
            {
                sb.Add($"S:{e.Order.OrderSize}");
            }
            if (e.Order.ExecutedPrice.HasValue)
            {
                sb.Add($"EP:{e.Order.ExecutedPrice}");
            }
            if (e.Order.ExecutedSize.HasValue)
            {
                sb.Add($"ES:{e.Order.ExecutedSize}");
            }
            Console.WriteLine(string.Join(' ', sb));
        }

        static void PrintPosition(BfxPosition pos)
        {
            /*if (pos.IsOpened)
            {
                Console.WriteLine($"{pos.Open.ToString(TimeFormat)} Position opened {pos.Side} P:{pos.OpenPrice} S:{pos.Size} TS:{_account.Positions.TotalSize}");
            }
            else // Closed
            {
                Console.WriteLine($"{pos.Close.Value.ToString(TimeFormat)} Position closed {pos.Side} P:{pos.ClosePrice} S:{pos.Size} TS:{_account.Positions.TotalSize} PT:{pos.Profit}");
            }*/
        }

        static void PrintOrder(BfxOrderStatus order)
        {

        }

        static void DumpResponse(BitFlyerResponse resp)
        {
            var jObj = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jObj, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }
    }
}
