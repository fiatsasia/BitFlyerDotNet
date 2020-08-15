//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
        const BfProductCode ProductCode = BfProductCode.FXBTCJPY;
        const decimal UnexecutableGap = 50000m;
        const string TimeFormat = "yyyy/MM/dd HH:mm:ss.ffff";

        static char GetCh(bool echo = true) { var ch = Char.ToUpper(Console.ReadKey(true).KeyChar); if (echo) Console.WriteLine(ch); return ch; }
        const char ESCAPE = (char)0x1b;

        static BfxAccount _account;
        static BfxMarket _market;
        static Dictionary<string, string> Properties;
        static ConcurrentDictionary<Guid, IBfxOrderTransaction> _transactions = new ConcurrentDictionary<Guid, IBfxOrderTransaction>();
        static decimal _orderSize;

        static void Main(string[] args)
        {
            LoadSettings(args[0]);

            using (_account = new BfxAccount(Properties["ApiKey"], Properties["ApiSecret"]))
            {
                _market = _account.GetMarket(ProductCode);
                _account.PositionChanged += OnPositionChanged;
                _orderSize = ProductCode.MinimumOrderSize();
                _market.OrderTransactionEvent += OnOrderTransactionEvent;

                _market.Open();
                while (true)
                {
                    Console.WriteLine("===================================================================");
                    Console.WriteLine("S)imple orders");
                    Console.WriteLine("C)onditional orders");
                    Console.WriteLine("F)ind orders");
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

                            case 'F':
                                FindOrders();
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
                var tran = _market.PlaceOrder(BfxOrder.MarketPrice(_account.Positions.Side.Opposite(), _account.Positions.TotalSize));
                _transactions[tran.Id] = tran;
            }
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
            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        static void OnPositionChanged(object sender, BfxPositionChangedEventArgs ev)
        {
            var pos = ev.Position;
            if (ev.IsOpened)
            {
                Console.WriteLine($"{pos.Open.ToString(TimeFormat)} Position opened {pos.Side} P:{pos.OpenPrice} S:{pos.Size} TS:{_account.Positions.TotalSize}");
            }
            else // Closed
            {
                Console.WriteLine($"{pos.Close.Value.ToString(TimeFormat)} Position closed {pos.Side} P:{pos.ClosePrice} S:{pos.Size} TS:{_account.Positions.TotalSize} PT:{pos.Profit} NP:{pos.NetProfit}");
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
                sb.Add(ev.EventType.ToString());
                if (ev.EventType == BfxOrderTransactionEventType.Canceled && _account.Positions.TotalSize > 0m)
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
    }
}
