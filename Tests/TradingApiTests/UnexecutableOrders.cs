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
        static async Task UnexecutableOrders(BfxApplication app)
        {
            var mds = await app.GetMarketDataSourceAsync(ProductCode);

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
                        await app.PlaceOrderAsync(BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, mds.BestBid - UnexecutableGap, _orderSize));
                        break;

                    case 'S': // Stop sell
                        await app.PlaceOrderAsync(BfOrderFactory.Stop(ProductCode, BfTradeSide.Sell, mds.BestAsk - UnexecutableGap, _orderSize));
                        break;

                    case 'I':
                        await app.PlaceOrderAsync(BfOrderFactory.IFD(
                            BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, mds.BestBid - UnexecutableGap, _orderSize),
                            BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, mds.BestAsk + UnexecutableGap, _orderSize)
                        ));
                        break;

                    case 'O':
                        await app.PlaceOrderAsync(BfOrderFactory.OCO(
                            BfOrderFactory.StopLimit(ProductCode, BfTradeSide.Sell, mds.LastTradedPrice + UnexecutableGap, mds.BestAsk + UnexecutableGap, _orderSize),
                            BfOrderFactory.StopLimit(ProductCode, BfTradeSide.Buy, mds.LastTradedPrice - UnexecutableGap, mds.BestBid -UnexecutableGap, _orderSize)
                        ));
                        break;

                    case '3':
                        await app.PlaceOrderAsync(BfOrderFactory.OCO(
                            BfOrderFactory.Limit(ProductCode, BfTradeSide.Buy, mds.BestBid - UnexecutableGap, _orderSize),
                            BfOrderFactory.Limit(ProductCode, BfTradeSide.Sell, mds.BestAsk + UnexecutableGap, _orderSize)));
                        break;

                    case '1':
                        await app.PlaceOrderAsync(BfOrderFactory.StopLimit(ProductCode, BfTradeSide.Sell, mds.BestAsk - UnexecutableGap, mds.BestAsk - UnexecutableGap, _orderSize));
                        break;

                    case 'C': CancelOrder(); break;
                    case 'X': ClosePositions(app); break;
                    case ESCAPE: return;
                }
            }
        }
    }
}
