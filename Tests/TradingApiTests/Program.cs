//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

global using System;
global using System.Linq;
global using System.Collections.Generic;
global using System.IO;
global using System.Xml.Linq;
global using System.Threading.Tasks;
global using System.Reactive.Linq;
global using Newtonsoft.Json;
global using BitFlyerDotNet.LightningApi;
global using BitFlyerDotNet.Trading;

namespace TradingApiTests;

partial class Program
{
    const string ProductCode = BfProductCode.FX_BTC_JPY;
    const decimal UnexecutableGap = 50000m;
    const string TimeFormat = "yyyy/MM/dd HH:mm:ss.ffff";
    const string OrderCacheFileName = "TradingApiTests.db3";
    const string TabString = "    ";

    static char GetCh(bool echo = true) { var ch = Char.ToUpper(Console.ReadKey(true).KeyChar); if (echo) Console.WriteLine(ch); return ch; }
    const char ESCAPE = (char)0x1b;

    static BfxApplication App { get; set; }
    static BfxMarketDataSource Mds { get; set; }
    static Dictionary<string, string> Properties;
    static decimal _orderSize;

    static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
        LoadSettings(args[0]);
        var key = Properties["ApiKey"];
        var secret = Properties["ApiSecret"];

        _orderSize = BfProductCode.GetMinimumOrderSize(ProductCode);

        var connStr = "data source=" + Path.Combine(Properties["CacheDirectoryPath"], OrderCacheFileName);
        using (App = new BfxApplication(key, secret))
        {
            App.AddTraceLoggingService(NLog.LogManager.GetLogger("debugOutput"));
            App.PositionChanged += OnPositionChanged;
            App.OrderChanged += OnOrderChanged;
            App.RealtimeSource.Channel.MessageSent += json => Console.WriteLine($"Socket message sent: {json}");
            App.RealtimeSource.Channel.MessageReceived += message => OnRealtimeMessageReceived(message);

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
                            await SimpleOrders();
                            break;

                        case 'C':
                            await ConditionalOrders();
                            break;

                        case 'U':
                            await UnexecutableOrders();
                            break;

                        case 'P':
                            GetActivePositions();
                            break;

                        case 'O':
                            await foreach (var order in App.GetRecentOrdersAsync(ProductCode, 20))
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

    static async void ClosePositions()
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

    static void OnPositionChanged(object sender, BfxPositionChangedEventArgs e)
    {
        PrintPosition(e.Position);
    }

    private static void OnOrderChanged(object sender, BfxOrderChangedEventArgs e)
    {
        var sb = new List<string>();
        sb.Add(e.Time.ToString(TimeFormat));

        if (e.EventType != BfxOrderEventType.ChildOrderChanged)
        {
            sb.Add(e.EventType.ToString());
        }
        else
        {
            sb.Add(e.EventType.ToString());
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

    static void PrintOrder(BfxOrder order)
    {
        var sb = new List<string>();
        if (order.OrderDate.HasValue)
        {
            sb.Add(order.OrderDate.Value.ToString(TimeFormat));
        }
        else
        {
            sb.Add("Order not triggered");
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
        Console.WriteLine(string.Join(' ', sb));

        for (int i = 0; i < order.Children.Count; i++)
        {
            Console.Write("    ");
            PrintOrder(order.Children[i]);
        }
    }

    static void OnRealtimeMessageReceived(object message)
    {
        switch (message)
        {
            case BfExecution[] execs:
                Console.WriteLine($"BfExecution[{execs.Length}]:");
                break;

            case BfTicker ticker:
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
    }

    static void DumpResponse(BitFlyerResponse resp)
    {
        var jObj = JsonConvert.DeserializeObject(resp.Json);
        Console.WriteLine(JsonConvert.SerializeObject(jObj, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
    }
}
