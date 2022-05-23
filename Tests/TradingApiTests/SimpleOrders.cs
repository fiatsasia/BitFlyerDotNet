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
                        var side = SelectSide();
                        if (side != BfTradeSide.Unknown)
                        {
                            await app.PlaceOrderAsync(BfxOrder.Limit(ProductCode, side, _market.CurrentPrice, _orderSize));
                        }
                        break;

                    case 'M':
                        switch (SelectSide())
                        {
                            case BfTradeSide.Buy:
                                PlaceOrder(BfxOrder.Market(ProductCode, BfTradeSide.Buy, _orderSize));
                                break;

                            case BfTradeSide.Sell:
                                PlaceOrder(BfxOrder.Market(ProductCode, BfTradeSide.Sell, _orderSize));
                                break;
                        }
                        break;

                    case 'T':
                        switch (SelectSide())
                        {
                            case BfTradeSide.Buy:
                                PlaceOrder(BfxOrder.Trailing(ProductCode, BfTradeSide.Buy, PnLGap * 2, _orderSize));
                                break;

                            case BfTradeSide.Sell:
                                PlaceOrder(BfxOrder.Trailing(ProductCode, BfTradeSide.Sell, PnLGap * 2, _orderSize));
                                break;
                        }
                        break;

                    case 'E':
                        PlaceOrder(BfxOrder.Limit(ProductCode, BfTradeSide.Buy, _market.BestBidPrice - UnexecutableGap, _orderSize), TimeSpan.FromMinutes(1), BfTimeInForce.NotSpecified);
                        break;

                    case 'F':
                        PlaceOrder(BfxOrder.Limit(ProductCode, BfTradeSide.Buy, _market.BestBidPrice - UnexecutableGap, _orderSize), TimeSpan.Zero, BfTimeInForce.FOK);
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
}
