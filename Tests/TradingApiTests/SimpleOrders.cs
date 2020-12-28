//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
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
        static void SimpleOrders()
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
                        switch(SelectSide())
                        {
                            case BfTradeSide.Buy:
                                PlaceOrder(BfxOrder.Limit(BfTradeSide.Buy, _market.BestBidPrice, _orderSize));
                                break;

                            case BfTradeSide.Sell:
                                PlaceOrder(BfxOrder.Limit(BfTradeSide.Sell, _market.BestAskPrice, _orderSize));
                                break;
                        }
                        break;

                    case 'M':
                        switch (SelectSide())
                        {
                            case BfTradeSide.Buy:
                                PlaceOrder(BfxOrder.Market(BfTradeSide.Buy, _orderSize));
                                break;

                            case BfTradeSide.Sell:
                                PlaceOrder(BfxOrder.Market(BfTradeSide.Sell, _orderSize));
                                break;
                        }
                        break;

                    case 'T':
                        switch (SelectSide())
                        {
                            case BfTradeSide.Buy:
                                PlaceOrder(BfxOrder.Trailing(BfTradeSide.Buy, PnLGap * 2, _orderSize));
                                break;

                            case BfTradeSide.Sell:
                                PlaceOrder(BfxOrder.Trailing(BfTradeSide.Sell, PnLGap * 2, _orderSize));
                                break;
                        }
                        break;

                    case 'E':
                        PlaceOrder(BfxOrder.Limit(BfTradeSide.Buy, _market.BestBidPrice - UnexecutableGap, _orderSize), TimeSpan.FromMinutes(1), BfTimeInForce.NotSpecified);
                        break;

                    case 'F':
                        PlaceOrder(BfxOrder.Limit(BfTradeSide.Buy, _market.BestBidPrice - UnexecutableGap, _orderSize), TimeSpan.Zero, BfTimeInForce.FOK);
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
