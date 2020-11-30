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
        static void UnexecutableOrders()
        {
            while (true)
            {
                Console.WriteLine("===================================================================");
                Console.WriteLine("L)imit order");
                Console.WriteLine("S)top Loss");
                Console.WriteLine("I)FD");
                Console.WriteLine("O)CO order");
                Console.WriteLine("1) Stop limit");
                Console.WriteLine("3) OCO market send");
                Console.WriteLine("");
                Console.WriteLine("C)ancel last order X) Close position");
                Console.Write("Main>Unexecutable Orders>");

                switch (GetCh())
                {
                    case 'L':
                        PlaceOrder(BfxOrder.Limit(BfTradeSide.Buy, _market.BestBidPrice - UnexecutableGap, _orderSize));
                        break;

                    case 'S': // Stop sell
                        PlaceOrder(BfxOrder.Stop(BfTradeSide.Sell, _market.BestAskPrice - UnexecutableGap, _orderSize));
                        break;

                    case 'I':
                        PlaceOrder(BfxOrder.IFD(
                            BfxOrder.Limit(BfTradeSide.Buy, _market.BestBidPrice - UnexecutableGap, _orderSize),
                            BfxOrder.Limit(BfTradeSide.Sell, _market.BestAskPrice + UnexecutableGap, _orderSize)
                        ));
                        break;

                    case 'O':
                        PlaceOrder(BfxOrder.OCO(
                            BfxOrder.StopLimit(BfTradeSide.Sell, _market.LastTradedPrice + UnexecutableGap, _market.BestAskPrice + UnexecutableGap, _orderSize),
                            BfxOrder.StopLimit(BfTradeSide.Buy, _market.LastTradedPrice - UnexecutableGap, _market.BestBidPrice -UnexecutableGap, _orderSize)
                        ));
                        break;

                    case '3':
                        PlaceOrder(BfxOrder.OCO(
                            BfxOrder.Limit(BfTradeSide.Buy, _market.BestBidPrice - UnexecutableGap, _orderSize),
                            BfxOrder.Limit(BfTradeSide.Sell, _market.BestAskPrice + UnexecutableGap, _orderSize)));
                        break;

                    case '1':
                        PlaceOrder(BfxOrder.StopLimit(BfTradeSide.Sell, _market.BestAskPrice - UnexecutableGap, _market.BestAskPrice - UnexecutableGap, _orderSize));
                        break;

                    case 'C': CancelOrder(); break;
                    case 'X': ClosePositions(); break;
                    case ESCAPE: return;
                }
            }
        }
    }
}
