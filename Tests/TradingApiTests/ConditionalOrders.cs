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
        const decimal PnLGap = 1000m;
        const decimal TrailingOffset = 3000m;

        static void ConditionalOrders()
        {
            while (true)
            {
                Console.WriteLine("I)FDOCO trailing");
                Console.WriteLine("O)CO unexecutable price");
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
                                PlaceOrder(BfxOrder.IFDOCO(
                                    BfxOrder.LimitPrice(BfTradeSide.Buy, _market.BestBidPrice, _orderSize),
                                    BfxOrder.Trailing(BfTradeSide.Sell, TrailingOffset, _orderSize),
                                    BfxOrder.Stop(BfTradeSide.Sell, _market.BestAskPrice - PnLGap, _orderSize)
                                ));
                                break;

                            case BfTradeSide.Sell:
                                PlaceOrder(BfxOrder.IFDOCO(
                                    BfxOrder.LimitPrice(BfTradeSide.Sell, _market.BestAskPrice, _orderSize),
                                    BfxOrder.Trailing(BfTradeSide.Buy, TrailingOffset, _orderSize),
                                    BfxOrder.Stop(BfTradeSide.Buy, _market.BestBidPrice + PnLGap, _orderSize)
                                ));
                                break;
                        }
                        break;

                    case 'O':
                        PlaceOrder(BfxOrder.OCO(
                            BfxOrder.LimitPrice(BfTradeSide.Sell, _market.BestAskPrice + UnexecutableGap, _orderSize),
                            BfxOrder.LimitPrice(BfTradeSide.Buy, _market.BestBidPrice - UnexecutableGap, _orderSize)
                        ));
                        break;

                    case 'L':
                        switch (SelectSide())
                        {
                            case BfTradeSide.Buy:
                                {
                                    var buyPrice = _market.BestBidPrice;
                                    PlaceOrder(BfxOrder.IFDOCO(
                                        BfxOrder.LimitPrice(BfTradeSide.Buy, buyPrice, _orderSize),
                                        BfxOrder.StopLimit(BfTradeSide.Sell, buyPrice - PnLGap, buyPrice - PnLGap, _orderSize),
                                        BfxOrder.LimitPrice(BfTradeSide.Sell, buyPrice + PnLGap, _orderSize)
                                    ));
                                }
                                break;

                            case BfTradeSide.Sell:
                                {
                                    var sellPrice = _market.BestAskPrice;
                                    PlaceOrder(BfxOrder.IFDOCO(
                                        BfxOrder.LimitPrice(BfTradeSide.Sell, sellPrice, _orderSize),
                                        BfxOrder.StopLimit(BfTradeSide.Buy, sellPrice + PnLGap, sellPrice + PnLGap, _orderSize),
                                        BfxOrder.LimitPrice(BfTradeSide.Buy, sellPrice - PnLGap, _orderSize)
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
                                    var buyPrice = _market.BestBidPrice;
                                    PlaceOrder(BfxOrder.IFDOCO(
                                        BfxOrder.LimitPrice(BfTradeSide.Buy, buyPrice, _orderSize),
                                        BfxOrder.Trailing(BfTradeSide.Sell, PnLGap, _orderSize),
                                        BfxOrder.LimitPrice(BfTradeSide.Sell, buyPrice + PnLGap * 2, _orderSize)
                                    ));
                                }
                                break;

                            case BfTradeSide.Sell:
                                {
                                    var sellPrice = _market.BestAskPrice;
                                    PlaceOrder(BfxOrder.IFDOCO(
                                        BfxOrder.LimitPrice(BfTradeSide.Sell, sellPrice, _orderSize),
                                        BfxOrder.Trailing(BfTradeSide.Buy, PnLGap, _orderSize),
                                        BfxOrder.LimitPrice(BfTradeSide.Buy, sellPrice - PnLGap * 2, _orderSize)
                                    ));
                                }
                                break;
                        }
                        break;

                    case 'E':
                        PlaceOrder(BfxOrder.OCO(
                            BfxOrder.LimitPrice(BfTradeSide.Sell, _market.BestAskPrice + UnexecutableGap, _orderSize),
                            BfxOrder.LimitPrice(BfTradeSide.Buy, _market.BestBidPrice - UnexecutableGap, _orderSize)),
                            TimeSpan.FromMinutes(1),
                            BfTimeInForce.NotSpecified
                        );
                        break;

                    case 'F':
                        PlaceOrder(BfxOrder.OCO(
                            BfxOrder.LimitPrice(BfTradeSide.Sell, _market.BestAskPrice + UnexecutableGap, _orderSize),
                            BfxOrder.LimitPrice(BfTradeSide.Buy, _market.BestBidPrice - UnexecutableGap, _orderSize)),
                            TimeSpan.Zero,
                            BfTimeInForce.FOK
                        );
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
