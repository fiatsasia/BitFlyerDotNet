//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using BitFlyerDotNet.LightningApi;
using Newtonsoft.Json;

namespace OrderApiTests
{
    partial class Program
    {
        static Queue<string> _parentOrderAcceptanceIds = new Queue<string>();

        static void ParentOrderMain()
        {
            while (true)
            {
                Console.WriteLine("======== Parent order operations");
                Console.WriteLine("S)top        Stop L)imit     T)rail");
                Console.WriteLine("I)FD         O)CO            IFD)OCO");
                Console.WriteLine("");
                Console.WriteLine("Time in F)orce");
                Console.WriteLine("M)inutes to expire");
                Console.WriteLine("");
                Console.WriteLine("C) Cancel parent order");
                Console.WriteLine("G)et parent orders");
                Console.WriteLine("");
                Console.WriteLine("R) Return to main");

                switch (GetCh())
                {
                    case 'S':
                        {
                            var request = BfParentOrderRequest.Stop(ProductCode, BfTradeSide.Buy, OrderSize, _ticker.BestAsk + UnexecuteGap);
                            var resp = _client.SendParentOrder(request);
                            if (resp.IsOk)
                            {
                                _parentOrderAcceptanceIds.Enqueue(resp.GetMessage().ParentOrderAcceptanceId);
                            }
                            else
                            {
                                Console.WriteLine($"Error: {resp.ErrorMessage}");
                            }
                        }
                        break;

                    case 'L':
                        {
                            var request = BfParentOrderRequest.StopLimit(ProductCode, BfTradeSide.Buy, OrderSize, _ticker.BestAsk + UnexecuteGap, _ticker.BestAsk + UnexecuteGap);
                            var resp = _client.SendParentOrder(request);
                            if (resp.IsOk)
                            {
                                _parentOrderAcceptanceIds.Enqueue(resp.GetMessage().ParentOrderAcceptanceId);
                            }
                            else
                            {
                                Console.WriteLine($"Error: {resp.ErrorMessage}");
                            }
                        }
                        break;

                    case 'T':
                        {
                            var request = BfParentOrderRequest.Trail(ProductCode, BfTradeSide.Buy, OrderSize, UnexecuteGap);
                            var resp = _client.SendParentOrder(request);
                            if (resp.IsOk)
                            {
                                _parentOrderAcceptanceIds.Enqueue(resp.GetMessage().ParentOrderAcceptanceId);
                            }
                            else
                            {
                                Console.WriteLine($"Error: {resp.ErrorMessage}");
                            }
                        }
                        break;

                    case 'I':
                        {
                            var request = BfParentOrderRequest.IFD(
                                BfParentOrderRequestParameter.LimitPrice(ProductCode, BfTradeSide.Sell, OrderSize, _ticker.BestAsk + UnexecuteGap),
                                BfParentOrderRequestParameter.LimitPrice(ProductCode, BfTradeSide.Buy, OrderSize, _ticker.BestBid - UnexecuteGap)
                            );
                            var resp = _client.SendParentOrder(request);
                            if (resp.IsOk)
                            {
                                _parentOrderAcceptanceIds.Enqueue(resp.GetMessage().ParentOrderAcceptanceId);
                            }
                            else
                            {
                                Console.WriteLine($"Error: {resp.ErrorMessage}");
                            }
                        }
                        break;

                    case 'O':
                        {
                            var request = BfParentOrderRequest.OCO(
                                BfParentOrderRequestParameter.LimitPrice(ProductCode, BfTradeSide.Sell, OrderSize, _ticker.BestAsk + UnexecuteGap),
                                BfParentOrderRequestParameter.LimitPrice(ProductCode, BfTradeSide.Buy, OrderSize, _ticker.BestBid - UnexecuteGap)
                            );
                            var resp = _client.SendParentOrder(request);
                            if (resp.IsOk)
                            {
                                _parentOrderAcceptanceIds.Enqueue(resp.GetMessage().ParentOrderAcceptanceId);
                            }
                            else
                            {
                                Console.WriteLine($"Error: {resp.ErrorMessage}");
                            }
                        }
                        break;

                    case 'D':
                        {
                            var request = BfParentOrderRequest.IFDOCO(
                                BfParentOrderRequestParameter.LimitPrice(ProductCode, BfTradeSide.Sell, OrderSize, _ticker.BestAsk + UnexecuteGap),
                                BfParentOrderRequestParameter.LimitPrice(ProductCode, BfTradeSide.Sell, OrderSize, _ticker.BestAsk + UnexecuteGap),
                                BfParentOrderRequestParameter.LimitPrice(ProductCode, BfTradeSide.Buy, OrderSize, _ticker.BestBid - UnexecuteGap)
                            );
                            var resp = _client.SendParentOrder(request);
                            if (resp.IsOk)
                            {
                                _parentOrderAcceptanceIds.Enqueue(resp.GetMessage().ParentOrderAcceptanceId);
                            }
                            else
                            {
                                Console.WriteLine($"Error: {resp.ErrorMessage}");
                            }
                        }
                        break;

                    case 'F':
                        break;

                    case 'M':
                        break;

                    case 'C':
                        {
                            var resp = _client.CancelParentOrder(ProductCode, parentOrderAcceptanceId: _parentOrderAcceptanceIds.Dequeue());
                            if (!resp.IsErrorOrEmpty)
                            {
                                // Cancel order sent
                            }
                        }
                        break;

                    case 'G':
                        {
                            var resp = _client.GetParentOrders(ProductCode);
                            if (resp.IsOk)
                            {
                                var jobj = JsonConvert.DeserializeObject(resp.Json);
                                var json = JsonConvert.SerializeObject(jobj, Formatting.Indented);
                                Console.WriteLine(json);
                            }
                        }
                        break;

                    case 'R':
                        return;
                }
            }
        }

        static void StopLimitImmediateExecute()
        {
            // StopLimitを発行する。
            // TriggerPriceを即時執行可能な価格にし、OrderPriceを乖離した価格にする。
        }
    }
}
