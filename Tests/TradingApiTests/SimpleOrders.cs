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
                Console.WriteLine("U)nexecutable limit price order");
                Console.WriteLine("E)xpire test");
                Console.WriteLine("T)ime in force test (FOK)");
                Console.WriteLine("C)ancel last order");
                Console.WriteLine();
                Console.Write("Main/Simple Orders>");

                switch (GetCh())
                {
                    case 'L':
                        switch(SelectSide())
                        {
                            case BfTradeSide.Buy:
                                _transactions.Enqueue(_market.PlaceOrder(BfxOrder.LimitPrice(BfTradeSide.Buy, _market.Ticker.BestBidPrice, _orderSize)));
                                break;

                            case BfTradeSide.Sell:
                                _transactions.Enqueue(_market.PlaceOrder(BfxOrder.LimitPrice(BfTradeSide.Sell, _market.Ticker.BestAskPrice, _orderSize)));
                                break;
                        }
                        break;

                    case 'M':
                        switch (SelectSide())
                        {
                            case BfTradeSide.Buy:
                                _transactions.Enqueue(_market.PlaceOrder(BfxOrder.MarketPrice(BfTradeSide.Buy, _orderSize)));
                                break;

                            case BfTradeSide.Sell:
                                _transactions.Enqueue(_market.PlaceOrder(BfxOrder.MarketPrice(BfTradeSide.Sell, _orderSize)));
                                break;
                        }
                        break;

                    case 'U':
                        switch (SelectSide())
                        {
                            case BfTradeSide.Buy:
                                _transactions.Enqueue(_market.PlaceOrder(BfxOrder.LimitPrice(BfTradeSide.Buy, _market.Ticker.BestBidPrice - UnexecutableGap, _orderSize)));
                                break;

                            case BfTradeSide.Sell:
                                _transactions.Enqueue(_market.PlaceOrder(BfxOrder.LimitPrice(BfTradeSide.Sell, _market.Ticker.BestAskPrice + UnexecutableGap, _orderSize)));
                                break;
                        }
                        break;

                    case 'E':
                        _transactions.Enqueue(_market.PlaceOrder(BfxOrder.LimitPrice(BfTradeSide.Buy, _market.Ticker.BestBidPrice - UnexecutableGap, _orderSize),
                            TimeSpan.FromMinutes(1),
                            BfTimeInForce.NotSpecified
                        ));
                        break;

                    case 'T':
                        _transactions.Enqueue(_market.PlaceOrder(BfxOrder.LimitPrice(BfTradeSide.Buy, _market.Ticker.BestBidPrice - UnexecutableGap, _orderSize),
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
