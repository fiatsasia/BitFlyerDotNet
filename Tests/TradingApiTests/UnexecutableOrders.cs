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
                    {
                        var order = Otm.Templates[0].CreateOrder(ProductCode, _orderSize, Mds.Ticker).Verify();
                    }
                    //await App.PlaceOrderAsync(BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, Mds.Ticker.BestBid - UnexecutableGap, _orderSize).Verify(Mds.Ticker));
                    break;

                case 'S': // Stop sell
                    {
                        var order = Otm.Templates[1].CreateOrder(ProductCode, _orderSize, Mds.Ticker).Verify();
                    }
                    //await App.PlaceOrderAsync(BfOrderFactory.Stop(ProductCode, BfTradeSide.Sell, Mds.Ticker.BestAsk - UnexecutableGap, _orderSize).Verify(Mds.Ticker));
                    break;

                case 'I':
                    await App.PlaceOrderAsync(BfOrderFactory.IFD(
                        BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, Mds.Ticker.BestBid - UnexecutableGap, _orderSize),
                        BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, Mds.Ticker.BestAsk + UnexecutableGap, _orderSize)
                    ).Verify());
                    break;

                case 'O':
                    await App.PlaceOrderAsync(BfOrderFactory.OCO(
                        BfOrderFactory.StopLimit(ProductCode, BfTradeSide.Sell, Mds.Ticker.LastTradedPrice + UnexecutableGap, Mds.Ticker.BestAsk + UnexecutableGap, _orderSize),
                        BfOrderFactory.StopLimit(ProductCode, BfTradeSide.Buy, Mds.Ticker.LastTradedPrice - UnexecutableGap, Mds.Ticker.BestBid -UnexecutableGap, _orderSize)
                    ).Verify());
                    break;

                case '3':
                    await App.PlaceOrderAsync(BfOrderFactory.OCO(
                        BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, Mds.Ticker.BestBid - UnexecutableGap, _orderSize).Verify(),
                        BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, Mds.Ticker.BestAsk + UnexecutableGap, _orderSize).Verify()
                    ).Verify());
                    break;

                case '1':
                    //
                    // Illegal order?
                    //
                    await App.PlaceOrderAsync(BfOrderFactory.StopLimit(ProductCode, BfTradeSide.Sell, Mds.Ticker.BestAsk - UnexecutableGap, Mds.Ticker.BestAsk - UnexecutableGap, _orderSize).Verify());
                    break;

                case 'C': await CancelOrder(); break;
                case 'X': ClosePositions(); break;
                case ESCAPE: return;
            }
        }
    }
}
