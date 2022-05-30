//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Threading.Tasks;
using BitFlyerDotNet.LightningApi;
using BitFlyerDotNet.Trading;

namespace TradingApiTests
{
    partial class Program
    {
        const decimal PnLGap = 1000m;
        const decimal TrailingOffset = 3000m;

        static async Task ConditionalOrders(BfxApplication app)
        {
            var mds = await app.GetMarketDataSourceAsync(ProductCode);

            while (true)
            {
                Console.WriteLine("I)FDOCO trailing");
                Console.WriteLine("L)imit IFDOCO");
                Console.WriteLine("T)railing IFDOCO");
                Console.WriteLine("E)xpire test");
                Console.WriteLine("F)OK test");
                Console.WriteLine("C)ancel last order");
                Console.WriteLine("X) Close position");
                Console.WriteLine();
                Console.Write("Main/Conditional Orders>");

                switch (GetCh())
                {
                    case 'I':
                        switch (SelectSide())
                        {
                            case BfTradeSide.Buy:
                                await app.PlaceOrderAsync(BfOrderFactory.IFDOCO(
                                    BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, mds.BestBid, _orderSize),
                                    BfOrderFactory.Trail(ProductCode, BfTradeSide.Sell, TrailingOffset, _orderSize),
                                    BfOrderFactory.Stop(ProductCode, BfTradeSide.Sell, mds.BestAsk - PnLGap, _orderSize)
                                ));
                                break;

                            case BfTradeSide.Sell:
                                await app.PlaceOrderAsync(BfOrderFactory.IFDOCO(
                                    BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, mds.BestAsk, _orderSize),
                                    BfOrderFactory.Trail(ProductCode, BfTradeSide.Buy, TrailingOffset, _orderSize),
                                    BfOrderFactory.Stop(ProductCode, BfTradeSide.Buy, mds.BestBid + PnLGap, _orderSize)
                                ));
                                break;
                        }
                        break;

                    case 'L':
                        switch (SelectSide())
                        {
                            case BfTradeSide.Buy:
                                {
                                    var buyPrice = mds.BestBid; // to prevent get difference price
                                    await app.PlaceOrderAsync(BfOrderFactory.IFDOCO(
                                        BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, buyPrice, _orderSize),
                                        BfOrderFactory.StopLimit(ProductCode, BfTradeSide.Sell, buyPrice - PnLGap, buyPrice - PnLGap, _orderSize),
                                        BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, buyPrice + PnLGap * 2m, _orderSize)
                                    ));
                                }
                                break;

                            case BfTradeSide.Sell:
                                {
                                    var sellPrice = mds.BestAsk;
                                    await app.PlaceOrderAsync(BfOrderFactory.IFDOCO(
                                        BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, sellPrice, _orderSize),
                                        BfOrderFactory.StopLimit(ProductCode, BfTradeSide.Buy, sellPrice + PnLGap, sellPrice + PnLGap, _orderSize),
                                        BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, sellPrice - PnLGap * 2m, _orderSize)
                                    ));
                                }
                                break;
                        }
                        break;

                    case 'T':
                        switch (SelectSide())
                        {
                            case BfTradeSide.Buy:
                                {
                                    var buyPrice = mds.BestBid;
                                    await app.PlaceOrderAsync(BfOrderFactory.IFDOCO(
                                        BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, buyPrice, _orderSize),
                                        BfOrderFactory.Trail(ProductCode, BfTradeSide.Sell, PnLGap, _orderSize),
                                        BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, buyPrice + PnLGap * 2, _orderSize)
                                    ));
                                }
                                break;

                            case BfTradeSide.Sell:
                                {
                                    var sellPrice = mds.BestAsk;
                                    await app.PlaceOrderAsync(BfOrderFactory.IFDOCO(
                                        BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, sellPrice, _orderSize),
                                        BfOrderFactory.Trail(ProductCode, BfTradeSide.Buy, PnLGap, _orderSize),
                                        BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, sellPrice - PnLGap * 2, _orderSize)
                                    ));
                                }
                                break;
                        }
                        break;

                    case 'E':
                        {
                            var order = BfOrderFactory.OCO(
                                BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, mds.BestAsk + UnexecutableGap, _orderSize),
                                BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, mds.BestBid - UnexecutableGap, _orderSize)
                            );
                            order.MinuteToExpire = 1;
                            await app.PlaceOrderAsync(order);
                        }
                        break;

                    case 'F':
                        {
                            var order = BfOrderFactory.OCO(
                                BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, mds.BestAsk + UnexecutableGap, _orderSize),
                                BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, mds.BestBid - UnexecutableGap, _orderSize)
                            );
                            order.TimeInForce = BfTimeInForce.FOK;
                            await app.PlaceOrderAsync(order);
                        }
                        break;

                    case 'C':
                        CancelOrder();
                        break;

                    case 'X':
                        ClosePositions(app);
                        break;

                    case ESCAPE:
                        return;
                }
            }
        }
    }
}
