//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Collections.Generic;
using System.Threading;
using BitFlyerDotNet.LightningApi;
using Newtonsoft.Json;

namespace OrderApiTests
{
    partial class Program
    {
        static Queue<string> _parentOrderAcceptanceIds = new Queue<string>();

        static async void ParentOrderMain()
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

                try
                {
                    switch (GetCh())
                    {
                        case 'S':
                            {
                                var order = BfOrderFactory.Stop(ProductCode, BfTradeSide.Buy, _ticker.BestAsk + UnexecuteGap, OrderSize);
                                var result = await _client.SendParentOrderAsync(order);
                                _parentOrderAcceptanceIds.Enqueue(result.ParentOrderAcceptanceId);
                            }
                            break;

                        case 'L':
                            {
                                var order = BfOrderFactory.StopLimit(ProductCode, BfTradeSide.Buy, _ticker.BestAsk + UnexecuteGap, _ticker.BestAsk + UnexecuteGap, OrderSize);
                                var result = await _client.SendParentOrderAsync(order);
                                _parentOrderAcceptanceIds.Enqueue(result.ParentOrderAcceptanceId);
                            }
                            break;

                        case 'T':
                            {
                                var order = BfOrderFactory.Trail(ProductCode, BfTradeSide.Buy, UnexecuteGap, OrderSize);
                                var result = await _client.SendParentOrderAsync(order);
                                _parentOrderAcceptanceIds.Enqueue(result.ParentOrderAcceptanceId);
                            }
                            break;

                        case 'I':
                            {
                                var order = BfOrderFactory.IFD(
                                    BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, _ticker.BestAsk + UnexecuteGap, OrderSize),
                                    BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, _ticker.BestBid - UnexecuteGap, OrderSize)
                                );
                                var result = await _client.SendParentOrderAsync(order);
                                _parentOrderAcceptanceIds.Enqueue(result.ParentOrderAcceptanceId);
                            }
                            break;

                        case 'O':
                            {
                                var order = BfOrderFactory.OCO(
                                    BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, _ticker.BestAsk + UnexecuteGap, OrderSize),
                                    BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, _ticker.BestBid - UnexecuteGap, OrderSize)
                                );
                                var result = await _client.SendParentOrderAsync(order);
                                _parentOrderAcceptanceIds.Enqueue(result.ParentOrderAcceptanceId);
                            }
                            break;

                        case 'D':
                            {
                                var order = BfOrderFactory.IFDOCO(
                                    BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, _ticker.BestAsk + UnexecuteGap, OrderSize),
                                    BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, _ticker.BestAsk + UnexecuteGap, OrderSize),
                                    BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, _ticker.BestBid - UnexecuteGap, OrderSize)
                                );
                                var result = await _client.SendParentOrderAsync(order);
                                _parentOrderAcceptanceIds.Enqueue(result.ParentOrderAcceptanceId);
                            }
                            break;

                        case 'F':
                            break;

                        case 'M':
                            break;

                        case 'C':
                            if (!await _client.CancelParentOrderAsync(ProductCode, parentOrderAcceptanceId: _parentOrderAcceptanceIds.Dequeue()))
                            {
                                // Cancel failed
                            }
                            break;

                        case 'G':
                            {
                                var resp = await _client.GetParentOrdersAsync(ProductCode, BfOrderState.Unknown, 0, 0, 0, CancellationToken.None);
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
                    Console.WriteLine($"{ex.Message}");
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
