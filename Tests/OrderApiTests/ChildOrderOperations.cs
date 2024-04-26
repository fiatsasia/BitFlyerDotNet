//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using BitFlyerDotNet.LightningApi;
using System.Threading;

namespace OrderApiTests
{
    partial class Program
    {
        static Queue<string> _childOrderAcceptanceIds = new Queue<string>();

        static async void ChildOrderMain()
        {
            while (true)
            {
                Console.WriteLine("======== Child order operations");
                Console.WriteLine("Market price S)ell order");
                Console.WriteLine("Market price B)uy order");
                Console.WriteLine("L)imit price order");
                Console.WriteLine("C)ancel order");
                Console.WriteLine("Cancel A)ll orders");
                Console.WriteLine("T)ime in force");
                Console.WriteLine("M)inutes to expire");
                Console.WriteLine("I)llegal size");
                Console.WriteLine("");
                Console.WriteLine("G)et child orders");
                Console.WriteLine("");
                Console.WriteLine("R) Return to main");

                try
                {
                    switch (GetCh())
                    {
                        case 'S':
                            {
                                var order = BfOrderFactory.Market(ProductCode, BfTradeSide.Sell, OrderSize);
                                var result = await _client.SendChildOrderAsync(order);
                                _childOrderAcceptanceIds.Enqueue(result.ChildOrderAcceptanceId);
                            }
                            break;

                        case 'B':
                            {
                                var order = BfOrderFactory.Market(ProductCode, BfTradeSide.Buy, OrderSize);
                                var result = await _client.SendChildOrderAsync(order);
                                _childOrderAcceptanceIds.Enqueue(result.ChildOrderAcceptanceId);
                            }
                            break;

                        case 'L':
                            {
                                var order = BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, _ticker.BestAsk + UnexecuteGap, OrderSize);
                                var content = await _client.SendChildOrderAsync(order);
                                _childOrderAcceptanceIds.Enqueue(content.ChildOrderAcceptanceId);
                            }
                            break;

                        case 'C':
                            if (!await _client.CancelChildOrderAsync(ProductCode, childOrderAcceptanceId: _childOrderAcceptanceIds.Dequeue()))
                            {
                                // Cancel failed
                            }
                            break;

                        case 'A':
                            if (await _client.CancelAllChildOrdersAsync(ProductCode))
                            {
                                _childOrderAcceptanceIds.Clear();
                            }
                            break;

                        case 'T':
                            {
                                var order = BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, _ticker.BestAsk + UnexecuteGap, OrderSize, timeInForce: BfTimeInForce.FOK);
                                var result = await _client.SendChildOrderAsync(order);
                                _childOrderAcceptanceIds.Enqueue(result.ChildOrderAcceptanceId);
                            }
                            break;

                        case 'M':
                            {
                                var order = BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, _ticker.BestAsk + UnexecuteGap, OrderSize, minuteToExpire: TimeSpan.FromMinutes(1));
                                var result = await _client.SendChildOrderAsync(order);
                                _childOrderAcceptanceIds.Enqueue(result.ChildOrderAcceptanceId);
                            }
                            break;

                        case 'I':
                            {
                                var order = BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, _ticker.BestAsk + UnexecuteGap, OrderSize);
                                var result = await _client.SendChildOrderAsync(order);
                                _childOrderAcceptanceIds.Enqueue(result.ChildOrderAcceptanceId);
                            }
                            break;

                        case 'G':
                            {
                                Console.WriteLine("1)Not specified 2)Canceled 3)Parent children 4)Parent canceled children");
                                Console.WriteLine("5)Child acceptance 6)Child acceptance canceled");
                                var orderState = BfOrderState.All;
                                var parentOrderId = "";
                                var childOrderAcceptanceId = "";
                                switch (GetCh())
                                {
                                    case '1':
                                        break;

                                    case '2':
                                        orderState = BfOrderState.Canceled;
                                        break;

                                    case '3':
                                        Console.Write("Parent order ID : ");
                                        parentOrderId = Console.ReadLine();
                                        break;

                                    case '4':
                                        Console.Write("Parent order ID : ");
                                        parentOrderId = Console.ReadLine();
                                        orderState = BfOrderState.Canceled;
                                        break;

                                    case '5':
                                        Console.Write("Child order acceptance ID : ");
                                        childOrderAcceptanceId = Console.ReadLine();
                                        break;

                                    case '6':
                                        Console.Write("Child order acceptance ID : ");
                                        childOrderAcceptanceId = Console.ReadLine();
                                        orderState = BfOrderState.Canceled;
                                        break;
                                }

                                BitFlyerResponse<BfChildOrderStatus[]> resp = null;
                                if (!string.IsNullOrEmpty(parentOrderId))
                                {
                                    resp = await _client.GetChildOrdersAsync(ProductCode, orderState, 0, 0, 0, null, null, parentOrderId, CancellationToken.None);
                                }
                                else if (!string.IsNullOrEmpty(childOrderAcceptanceId))
                                {
                                    resp = await _client.GetChildOrdersAsync(ProductCode, orderState, 0, 0, 0, null, childOrderAcceptanceId, null, CancellationToken.None);
                                }
                                else
                                {
                                    resp = await _client.GetChildOrdersAsync(ProductCode, orderState, 0, 0, 0, null, null, null, CancellationToken.None);
                                }

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
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
