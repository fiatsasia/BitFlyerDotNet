//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
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
                Console.WriteLine("S)top");
                Console.WriteLine("O)Stop limit");
                Console.WriteLine("T)railing");
                Console.WriteLine("E)xpire test");
                Console.WriteLine("F)OK");
                Console.WriteLine("U)nexecutable order");
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
                                PlaceOrder(BfxOrder.LimitPrice(BfTradeSide.Buy, _market.BestBidPrice, _orderSize));
                                break;

                            case BfTradeSide.Sell:
                                PlaceOrder(BfxOrder.LimitPrice(BfTradeSide.Sell, _market.BestAskPrice, _orderSize));
                                break;
                        }
                        break;

                    case 'M':
                        switch (SelectSide())
                        {
                            case BfTradeSide.Buy:
                                PlaceOrder(BfxOrder.MarketPrice(BfTradeSide.Buy, _orderSize));
                                break;

                            case BfTradeSide.Sell:
                                PlaceOrder(BfxOrder.MarketPrice(BfTradeSide.Sell, _orderSize));
                                break;
                        }
                        break;

                    case 'S':
                        PlaceOrder(BfxOrder.Stop(BfTradeSide.Sell, _market.BestAskPrice + UnexecutableGap, _orderSize));
                        break;

                    case 'O':
                        PlaceOrder(BfxOrder.StopLimit(BfTradeSide.Sell, _market.BestAskPrice + UnexecutableGap, _market.BestAskPrice + UnexecutableGap, _orderSize));
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
                        PlaceOrder(BfxOrder.LimitPrice(BfTradeSide.Buy, _market.BestBidPrice - UnexecutableGap, _orderSize), TimeSpan.FromMinutes(1), BfTimeInForce.NotSpecified);
                        break;

                    case 'F':
                        PlaceOrder(BfxOrder.LimitPrice(BfTradeSide.Buy, _market.BestBidPrice - UnexecutableGap, _orderSize), TimeSpan.Zero, BfTimeInForce.FOK);
                        break;

                    case 'U':
                        PlaceOrder(BfxOrder.LimitPrice(BfTradeSide.Buy, _market.BestBidPrice - UnexecutableGap, _orderSize));
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
