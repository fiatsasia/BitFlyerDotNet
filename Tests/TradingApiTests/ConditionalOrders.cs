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
        const decimal PnLGap = 500m;

        static void ConditionalOrders()
        {
            while (true)
            {
                Console.WriteLine("S)top sell unexecutable price");
                Console.WriteLine("I)FD unexecutable price");
                Console.WriteLine("O)CO unexecutable price");
                Console.WriteLine("L)imit IFDOCO");
                Console.WriteLine("E)xpire test");
                Console.WriteLine("T)ime in force test (FOK)");
                Console.WriteLine("C)ancel last order");
                Console.WriteLine();
                Console.Write("Main/Conditional Orders>");

                switch (GetCh())
                {
                    case 'S':
                        _transactions.Enqueue(_market.PlaceOrder(BfxOrder.StopLimit(
                            BfTradeSide.Sell,
                            _market.Ticker.BestAskPrice + UnexecutableGap,
                            _market.Ticker.BestAskPrice + UnexecutableGap,
                            _orderSize
                        )));
                        break;

                    case 'I':
                        _transactions.Enqueue(_market.PlaceOrder(BfxOrder.IFD(
                            BfxOrder.LimitPrice(
                                BfTradeSide.Sell,
                                _market.Ticker.BestAskPrice + UnexecutableGap,
                                _orderSize
                            ),
                            BfxOrder.StopLimit(
                                BfTradeSide.Sell,
                                _market.Ticker.BestAskPrice + UnexecutableGap,
                                _market.Ticker.BestAskPrice + UnexecutableGap,
                                _orderSize
                            )
                        )));
                        break;

                    case 'O':
                        _transactions.Enqueue(_market.PlaceOrder(BfxOrder.OCO(
                            BfxOrder.LimitPrice(
                                BfTradeSide.Sell,
                                _market.Ticker.BestAskPrice + UnexecutableGap,
                                _orderSize
                            ),
                            BfxOrder.LimitPrice(
                                BfTradeSide.Buy,
                                _market.Ticker.BestBidPrice - UnexecutableGap,
                                _orderSize
                            )
                        )));
                        break;

                    case 'L':
                        switch (SelectSide())
                        {
                            case BfTradeSide.Buy:
                                {
                                    var buyPrice = _market.Ticker.BestBidPrice;
                                    _transactions.Enqueue(_market.PlaceOrder(BfxOrder.IFDOCO(
                                        BfxOrder.LimitPrice(BfTradeSide.Buy, buyPrice, _orderSize),
                                        BfxOrder.StopLimit(BfTradeSide.Sell, buyPrice - PnLGap, buyPrice - PnLGap, _orderSize),
                                        BfxOrder.LimitPrice(BfTradeSide.Sell, buyPrice + PnLGap, _orderSize)
                                    )));
                                }
                                break;

                            case BfTradeSide.Sell:
                                {
                                    var sellPrice = _market.Ticker.BestAskPrice;
                                    _transactions.Enqueue(_market.PlaceOrder(BfxOrder.IFDOCO(
                                        BfxOrder.LimitPrice(BfTradeSide.Sell, sellPrice, _orderSize),
                                        BfxOrder.StopLimit(BfTradeSide.Buy, sellPrice + PnLGap, sellPrice + PnLGap, _orderSize),
                                        BfxOrder.LimitPrice(BfTradeSide.Buy, sellPrice - PnLGap, _orderSize)
                                    )));
                                }
                                break;
                        }
                        break;

                    case 'E':
                        _transactions.Enqueue(_market.PlaceOrder(BfxOrder.StopLimit(
                                BfTradeSide.Sell,
                                _market.Ticker.BestAskPrice + UnexecutableGap,
                                _market.Ticker.BestAskPrice + UnexecutableGap,
                                _orderSize
                            ),
                            TimeSpan.FromMinutes(1),
                            BfTimeInForce.NotSpecified
                        ));
                        break;

                    case 'T':
                        _transactions.Enqueue(_market.PlaceOrder(BfxOrder.StopLimit(
                                BfTradeSide.Sell,
                                _market.Ticker.BestAskPrice + UnexecutableGap,
                                _market.Ticker.BestAskPrice + UnexecutableGap,
                                _orderSize
                            ),
                            TimeSpan.Zero,
                            BfTimeInForce.FOK
                        ));
                        break;

                    case 'C':
                        _transactions.Dequeue().Cancel();
                        break;

                    case ESCAPE:
                        return;
                }
            }
        }
    }
}
