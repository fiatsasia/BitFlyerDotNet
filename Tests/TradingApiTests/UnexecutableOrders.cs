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
    static async Task UnexecutableOrders()
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
                    await App.PlaceOrderAsync(BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, Mds.BestBid - UnexecutableGap, _orderSize));
                    break;

                case 'S': // Stop sell
                    await App.PlaceOrderAsync(BfOrderFactory.Stop(ProductCode, BfTradeSide.Sell, Mds.BestAsk - UnexecutableGap, _orderSize));
                    break;

                case 'I':
                    await App.PlaceOrderAsync(BfOrderFactory.IFD(
                        BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, Mds.BestBid - UnexecutableGap, _orderSize),
                        BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, Mds.BestAsk + UnexecutableGap, _orderSize)
                    ));
                    break;

                case 'O':
                    await App.PlaceOrderAsync(BfOrderFactory.OCO(
                        BfOrderFactory.StopLimit(ProductCode, BfTradeSide.Sell, Mds.LastTradedPrice + UnexecutableGap, Mds.BestAsk + UnexecutableGap, _orderSize),
                        BfOrderFactory.StopLimit(ProductCode, BfTradeSide.Buy, Mds.LastTradedPrice - UnexecutableGap, Mds.BestBid -UnexecutableGap, _orderSize)
                    ));
                    break;

                case '3':
                    await App.PlaceOrderAsync(BfOrderFactory.OCO(
                        BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, Mds.BestBid - UnexecutableGap, _orderSize),
                        BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, Mds.BestAsk + UnexecutableGap, _orderSize)));
                    break;

                case '1':
                    await App.PlaceOrderAsync(BfOrderFactory.StopLimit(ProductCode, BfTradeSide.Sell, Mds.BestAsk - UnexecutableGap, Mds.BestAsk - UnexecutableGap, _orderSize));
                    break;

                case 'C': CancelOrder(); break;
                case 'X': ClosePositions(); break;
                case ESCAPE: return;
            }
        }
    }
}
