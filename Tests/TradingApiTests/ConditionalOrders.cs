//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace TradingApiTests;

partial class Program
{
    const decimal PnLGap = 1000m;
    const decimal TrailingOffset = 3000m;

    static async Task ConditionalOrders()
    {
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
                            await App.PlaceOrderAsync(BfOrderFactory.IFDOCO(
                                BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, Mds.BestBid, _orderSize),
                                BfOrderFactory.Trail(ProductCode, BfTradeSide.Sell, TrailingOffset, _orderSize),
                                BfOrderFactory.Stop(ProductCode, BfTradeSide.Sell, Mds.BestAsk - PnLGap, _orderSize)
                            ));
                            break;

                        case BfTradeSide.Sell:
                            await App.PlaceOrderAsync(BfOrderFactory.IFDOCO(
                                BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, Mds.BestAsk, _orderSize),
                                BfOrderFactory.Trail(ProductCode, BfTradeSide.Buy, TrailingOffset, _orderSize),
                                BfOrderFactory.Stop(ProductCode, BfTradeSide.Buy, Mds.BestBid + PnLGap, _orderSize)
                            ));
                            break;
                    }
                    break;

                case 'L':
                    switch (SelectSide())
                    {
                        case BfTradeSide.Buy:
                            {
                                var buyPrice = Mds.BestBid; // to prevent get difference price
                                await App.PlaceOrderAsync(BfOrderFactory.IFDOCO(
                                    BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, buyPrice, _orderSize),
                                    BfOrderFactory.StopLimit(ProductCode, BfTradeSide.Sell, buyPrice - PnLGap, buyPrice - PnLGap, _orderSize),
                                    BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, buyPrice + PnLGap * 2m, _orderSize)
                                ));
                            }
                            break;

                        case BfTradeSide.Sell:
                            {
                                var sellPrice = Mds.BestAsk;
                                await App.PlaceOrderAsync(BfOrderFactory.IFDOCO(
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
                                var buyPrice = Mds.BestBid;
                                await App.PlaceOrderAsync(BfOrderFactory.IFDOCO(
                                    BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, buyPrice, _orderSize),
                                    BfOrderFactory.Trail(ProductCode, BfTradeSide.Sell, PnLGap, _orderSize),
                                    BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, buyPrice + PnLGap * 2, _orderSize)
                                ));
                            }
                            break;

                        case BfTradeSide.Sell:
                            {
                                var sellPrice = Mds.BestAsk;
                                await App.PlaceOrderAsync(BfOrderFactory.IFDOCO(
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
                            BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, Mds.BestAsk + UnexecutableGap, _orderSize),
                            BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, Mds.BestBid - UnexecutableGap, _orderSize)
                        );
                        order.MinuteToExpire = 1;
                        await App.PlaceOrderAsync(order);
                    }
                    break;

                case 'F':
                    {
                        var order = BfOrderFactory.OCO(
                            BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, Mds.BestAsk + UnexecutableGap, _orderSize),
                            BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, Mds.BestBid - UnexecutableGap, _orderSize)
                        );
                        order.TimeInForce = BfTimeInForce.FOK;
                        await App.PlaceOrderAsync(order);
                    }
                    break;

                case 'C':
                    CancelOrder();
                    break;

                case 'X':
                    ClosePositions();
                    break;

                case ESCAPE:
                    return;
            }
        }
    }
}
