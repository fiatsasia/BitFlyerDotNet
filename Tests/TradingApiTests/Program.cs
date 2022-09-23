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
    const string TimeFormat = "yyyy/MM/dd HH:mm:ss.ffff";
    const string OrderCacheFileName = "TradingApiTests.db";

    static char GetCh(bool echo = true) { var ch = Char.ToUpper(Console.ReadKey(true).KeyChar); if (echo) Console.WriteLine(ch); return ch; }
    const char ESCAPE = (char)0x1b;

    static BfxApplication App { get; set; }
    static Dictionary<string, string> Properties;
    static decimal _orderSize;

    static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
        LoadSettings(args[0]);
        var key = Properties["ApiKey"];
        var secret = Properties["ApiSecret"];

        _orderSize = BfProductCode.GetMinimumOrderSize(ProductCode);

        var config = new BfxConfiguration
        {
            CacheDirectoryPath = Path.Combine(Properties["CacheDirectoryPath"], OrderCacheFileName).Replace("%LOCALAPPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)),
        };
        using (App = new BfxApplication(config, key, secret))
        {
            App.AddTraceLoggingService(NLog.LogManager.GetLogger("debugOutput"));
            App.AddOrderTemplates("orderTemplates.json");
            // App.AddDataSource(new BitFlyerDotNet.DataSource.SQLite());

            App.PositionChanged += OnPositionChanged;
            App.OrderChanged += OnOrderChanged;
            App.RealtimeSource.Channel.MessageSent += json => Console.WriteLine($"Socket message sent: {json}");
            App.RealtimeSource.Channel.MessageReceived += message => OnRealtimeMessageReceived(message);


            while (true)
            {
                Console.WriteLine("===================================================================");
                Console.WriteLine("Place o)rder");
                Console.WriteLine("C)ancel last order");
                Console.WriteLine("X) Close position");
                Console.WriteLine("");
                Console.WriteLine("R)ecent orders");
                Console.WriteLine("A)ctive orders");
                Console.WriteLine("T)oday's Profit");
                Console.WriteLine("Active P)ositions");
                Console.WriteLine("");
                Console.Write("Main>");

                try
                {
                    switch (GetCh())
                    {
                        case 'O':
                            {
                                var templ = App.GetOrderTemplates();
                                for (int i = 0; i < templ.Length; i++)
                                {
                                    Console.WriteLine($"{i}) {templ[i].Description}");
                                }
                                Console.WriteLine();
                                Console.Write("Select conditional order>");
                                var line = Console.ReadLine();
                                if (string.IsNullOrEmpty(line))
                                {
                                    break;
                                }
                                var selectedNo = int.Parse(line);
                                var order = await templ[selectedNo].CreateOrderAsync(ProductCode, _orderSize);
                                Console.WriteLine(JsonConvert.SerializeObject(order, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));

                                try
                                {
                                    order.Verify();
                                    await App.VerifyOrderDefaultAsync(order);
                                    await App.PlaceOrderAsync(order);
                                }
                                catch (ArgumentException ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }
                            break;

                        case 'A':
                            await foreach (var order in App.GetActiveOrdersAsync(ProductCode))
                            {
                                PrintOrder(order);
                            }
                            break;

                        case 'C':
                            await CancelOrder();
                            break;

                        case 'X':
                            ClosePositions();
                            break;

                        case 'P':
                            await GetActivePositions();
                            break;

                        case 'R':
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
                    Log.Error(ex);
                }
            }
        }
    }

    static void LoadSettings(string filePath)
    {
        var xml = XDocument.Load(filePath);
        Properties = xml.Element("RunSettings").Element("TestRunParameters").Elements("Parameter").ToDictionary(e => e.Attribute("name").Value, e => e.Attribute("value").Value);
    }

    static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
    {
        Console.WriteLine(e.ExceptionObject.ToString());
        Console.WriteLine("Press Enter to continue");
        Console.ReadLine();
        Environment.Exit(1);
    }

    static async Task CancelOrder()
    {
        await foreach (var order in App.GetActiveOrdersAsync(ProductCode))
        {
            await App.CancelOrderAsync(ProductCode, order.OrderAcceptanceId);
        }
    }

    static async void ClosePositions()
    {
        /*if (_account.Positions.TotalSize > 0m)
        {
            await app.PlaceOrderAsync(BfChildOrderRequest.Market(ProductCode, _account.Positions.Side.GetOpposite(), _account.Positions.TotalSize));
        }*/
    }

    static string ToDisplayString(BfxPosition pos)
    {
        if (pos.IsOpened)
        {
            return $"{pos.OpenTime.ToString(TimeFormat)} Position opened {pos.Side} P:{pos.OpenPrice} S:{pos.Size}";
        }
        else // Closed
        {
            return $"{pos.CloseTime.Value.ToString(TimeFormat)} Position closed {pos.Side} P:{pos.ClosePrice} S:{pos.Size} PT:{pos.Profit}";
        }
    }

    static async Task GetActivePositions()
    {
        await foreach (var pos in App.GetActivePositions(ProductCode))
        {
            Console.WriteLine(ToDisplayString(pos));
        }
    }

    static void OnPositionChanged(object sender, BfxPositionChangedEventArgs e)
    {
        var pos = e.Position;
        Console.WriteLine($"{ToDisplayString(pos)} TS:{e.TotalSize}");
    }

    private static void OnOrderChanged(object sender, BfxOrderChangedEventArgs e)
    {
        var sb = new List<string>();
        sb.Add(e.Time.ToString(TimeFormat));
        sb.Add($"{e.Order.ProductCode}");
        sb.Add($"{e.Order.OrderType}");
        sb.Add(e.EventType.ToString());

        switch (e.EventType)
        {
            case BfxOrderEventType.OrderAccepted:
                sb.Add(e.Order.OrderAcceptanceId);
                break;
        }

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

        var msg = string.Join(' ', sb);
        Console.WriteLine(msg);
        Log.Info(msg);
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
        sb.Add(order.OrderAcceptanceId);
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
}
