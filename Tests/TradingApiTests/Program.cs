//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
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
using BitFlyerDotNet.LightningApi;
using BitFlyerDotNet.Trading;
using BitFlyerDotNet.Historical;
using Newtonsoft.Json;

namespace TradingApiTests
{
    partial class Program
    {
        const BfProductCode ProductCode = BfProductCode.FXBTCJPY;
        const decimal UnexecutableGap = 50000m;
        const string TimeFormat = "yyyy/MM/dd HH:mm:ss.ffff";
        const string OrderCacheFileName = "TradingApiTests.db3";
        const string TabString = "    ";

        static char GetCh(bool echo = true) { var ch = Char.ToUpper(Console.ReadKey(true).KeyChar); if (echo) Console.WriteLine(ch); return ch; }
        const char ESCAPE = (char)0x1b;

        static BfxAccount _account;
        static BfxMarket _market;
        static OrderSource _orderCache;
        static Dictionary<string, string> Properties;
        static ConcurrentDictionary<Guid, IBfxTransaction> _transactions = new ConcurrentDictionary<Guid, IBfxTransaction>();
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

            var connStr = "data source=" + Path.Combine(Properties["CacheDirectoryPath"], OrderCacheFileName);
            using (_account = new BfxAccount(key, secret))
            {
                _market = await _account.GetMarketAsync(ProductCode);
                _account.PositionChanged += OnPositionChanged;
                _orderSize = ProductCode.GetMinimumOrderSize();
                _market.OrderTransactionChanged += OnOrderTransactionChanged;


                await _account.OpenAsync();
                _orderCache = new OrderSource(_account.Client, connStr, ProductCode);
                _market.Open(_orderCache);
                _market.GetActiveTransactions().ForEach(e => _transactions[Guid.NewGuid()] = e);
                while (true)
                {
                    Console.WriteLine("===================================================================");
                    Console.WriteLine("S)imple orders");
                    Console.WriteLine("C)onditional orders");
                    Console.WriteLine("U)nexecutable orders");
                    Console.WriteLine("");
                    Console.WriteLine("Active O)rders");
                    Console.WriteLine("T)oday's Profit");
                    Console.WriteLine("F)ind child orders");
                    Console.WriteLine("Active P)ositions");
                    Console.WriteLine("R)etrieve child orders");
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

                            case 'U':
                                UnexecutableOrders();
                                break;

                            case 'F':
                                FindOrders();
                                break;

                            case 'P':
                                GetActivePositions();
                                break;

                            case 'O':
                                GetActiveOrders();
                                break;

                            case 'R':
                                RetrieveChildOrders();
                                break;

                            case 'T':
                                Console.WriteLine($"Profit {_orderCache.CalculateProfit(TimeSpan.FromDays(1))} JPY");
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

        static void PlaceOrder(IBfxOrder order)
        {
            var tran = _market.PlaceOrder(order);
            _transactions[tran.Id] = tran;
        }

        static void PlaceOrder(IBfxOrder order, TimeSpan timeToExpore, BfTimeInForce timeInForce)
        {
            var tran = _market.PlaceOrder(order, timeToExpore, timeInForce);
            _transactions[tran.Id] = tran;
        }

        static void CancelOrder()
        {
            var tran = _transactions.Values.Where(e => e.IsCancelable).OrderBy(e => e.OpenTime).FirstOrDefault();
            if (tran != null)
            {
                tran.Cancel();
            }
        }

        static void ClosePositions()
        {
            if (_account.Positions.TotalSize > 0m)
            {
                var tran = _market.PlaceOrder(BfxOrder.Market(_account.Positions.Side.GetOpposite(), _account.Positions.TotalSize));
                _transactions[tran.Id] = tran;
            }
        }

        static void GetActiveOrders()
        {
            Console.Write("C)hild P)arent I)nternal : ");
            while (true)
            {
                switch (GetCh())
                {
                    case 'I':
                        GetInternalActiveOrders();
                        return;

                    case 'C':
                        GetActiveChildOrders();
                        return;

                    case 'P':
                        GetActiveParentOrders();
                        return;

                    case ESCAPE:
                        return;
                }
            }
        }

        static void GetInternalActiveOrders()
        {
            foreach (var order in _market.GetActiveTransactions().Select(e => e.Order))
            {
                Console.WriteLine($"{order.OrderDate} {order.OrderType} {order.State}");
            }
        }

        static void GetActiveParentOrders()
        {
            var parents = _orderCache.GetActiveParentOrders();
            foreach (var parent in parents)
            {
                Console.WriteLine($"{parent.OrderDate} {parent.OrderType}");
                foreach (var child in parent.Children)
                {
                    Console.WriteLine(TabString + $"{child.OrderDate} {child.OrderType} {child.Side}");
                    foreach (var exec in child.Executions)
                    {
                        Console.WriteLine(TabString + TabString + $"{exec.ExecutedTime} P:{exec.Price} S:{exec.Size}");
                    }
                }
            }
        }

        static void GetActiveChildOrders()
        {
            var children = _orderCache.GetActiveIndependentChildOrders();
            foreach (var child in children)
            {
                Console.WriteLine($"{child.OrderDate} {child.OrderType} {child.Side}");
                foreach (var exec in child.Executions)
                {
                    Console.WriteLine(TabString + $"{exec.ExecutedTime} P:{exec.Price} S:{exec.Size}");
                }
            }
        }

        static void GetActivePositions()
        {
            _account.Positions.GetActivePositions().ForEach(e => PrintPosition(e));
            DumpResponse(_account.Client.GetPositions(ProductCode));
        }

        static void FindOrders()
        {
            Console.Write("Child order acceptance ID : ");
            var coai = Console.ReadLine();
            Console.Write("A)ctive Comp)leted C)anceled E)xpired R)ejected");
            BfOrderState state;
            switch (GetCh())
            {
                case 'A':
                    state = BfOrderState.Active;
                    break;

                case 'P':
                    state = BfOrderState.Completed;
                    break;

                case 'C':
                    state = BfOrderState.Canceled;
                    break;

                case 'E':
                    state = BfOrderState.Expired;
                    break;

                case 'R':
                    state = BfOrderState.Rejected;
                    break;

                default:
                    state = BfOrderState.Unknown;
                    break;
            }

            var resp = _account.Client.GetChildOrders(ProductCode, orderState: state, childOrderAcceptanceId: coai);
            var jObj = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jObj, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        static string _parentOrderId;
        static void RetrieveChildOrders()
        {
            if (string.IsNullOrEmpty(_parentOrderId))
            {
                var parentOrder = _account.Client.GetParentOrders(ProductCode, BfOrderState.Active).GetContent().FirstOrDefault();
                if (parentOrder == null)
                {
                    return;
                }
                _parentOrderId = parentOrder.ParentOrderId;
            }
            DumpResponse(_account.Client.GetChildOrders(ProductCode, parentOrderId: _parentOrderId));
        }

        static void OnPositionChanged(object sender, BfxPositionEventArgs ev)
        {
            PrintPosition(ev.Position);
        }

        private static void OnOrderTransactionChanged(object sender, BfxOrderTransactionEventArgs ev)
        {
            var sb = new List<string>();
            sb.Add(ev.Time.ToString(TimeFormat));

            IBfxOrder order;
            if (ev.EventType != BfxTransactionEventType.ChildOrderEvent)
            {
                order = ev.Order;
                sb.Add(ev.EventType.ToString());
                if (ev.EventType == BfxTransactionEventType.Canceled && _account.Positions.TotalSize > 0m)
                {
                    Task.Run(() =>
                    {
                        Console.WriteLine($"Position is alive. size:{_account.Positions.TotalSize}");
                        Console.Write("Close positions ? (y/n)");
                        if (GetCh() == 'Y')
                        {
                            ClosePositions();
                        }
                    });
                }
            }
            else
            {
                order = ev.Order.Children[ev.ChildOrderIndex];
                sb.Add(ev.ChildEventType.ToString());
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
        }

        static void PrintPosition(BfxPosition pos)
        {
            if (pos.IsOpened)
            {
                Console.WriteLine($"{pos.Open.ToString(TimeFormat)} Position opened {pos.Side} P:{pos.OpenPrice} S:{pos.Size} TS:{_account.Positions.TotalSize}");
            }
            else // Closed
            {
                Console.WriteLine($"{pos.Close.Value.ToString(TimeFormat)} Position closed {pos.Side} P:{pos.ClosePrice} S:{pos.Size} TS:{_account.Positions.TotalSize} PT:{pos.Profit}");
            }
        }

        static void PrintOrder(IBfxOrder order)
        {

        }

        static void DumpResponse(IBitFlyerResponse resp)
        {
            var jObj = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jObj, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }
    }
}
