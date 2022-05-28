//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using BitFlyerDotNet.LightningApi;
using BitFlyerDotNet.Trading;

namespace TradingApiTests
{
    partial class Program
    {
        static async void SimpleOrders(BfxApplication app)
        {
            var mds = await app.GetMarketDataSourceAsync(ProductCode);

            while (true)
            {
                Console.WriteLine("L)imit price order best ask/bid price");
                Console.WriteLine("M)arket price order");
                Console.WriteLine("T)railing");
                Console.WriteLine("E)xpire test");
                Console.WriteLine("F)OK");
                Console.WriteLine("C)ancel last order");
                Console.WriteLine("X) Close position");
                Console.WriteLine();
                Console.Write("Main/Simple Orders>");

                switch (GetCh())
                {
                    case 'L':
                        {
                            var side = SelectSide();
                            if (side != BfTradeSide.Unknown)
                            {
                                await app.PlaceOrderAsync(BfOrderFactory.Limit(ProductCode, side, mds.LastTradedPrice, _orderSize));
                            }
                        }
                        break;

                    case 'M':
                        {
                            var side = SelectSide();
                            if (side != BfTradeSide.Unknown)
                            {
                                await app.PlaceOrderAsync(BfOrderFactory.Market(ProductCode, side, mds.LastTradedPrice));
                            }
                        }
                        break;

                    case 'T':
                        {
                            var side = SelectSide();
                            if (side != BfTradeSide.Unknown)
                            {
                                await app.PlaceOrderAsync(BfOrderFactory.Trail(ProductCode, side, PnLGap * 2, _orderSize));
                            }
                        }
                        break;

                    case 'E':
                        await app.PlaceOrderAsync(BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, mds.BestBid - UnexecutableGap, _orderSize, minuteToExpire: 1));
                        break;

                    case 'F':
                        await app.PlaceOrderAsync(BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, mds.BestBid - UnexecutableGap, _orderSize, timeInForce: BfTimeInForce.FOK));
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
