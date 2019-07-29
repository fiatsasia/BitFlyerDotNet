//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using BitFlyerDotNet.LightningApi;
using BitFlyerDotNet.Trading;

namespace TradingApiSample
{
    partial class Program
    {
        static BfxChildOrderTransaction _childOrderTransaction;

        static void ChildOrderMain()
        {
            while (true)
            {
                Console.WriteLine("======== Child order operations");
                Console.WriteLine("1) Place unexecutable child order");
                Console.WriteLine("2) FOK (Fill Or Kill)");
                Console.WriteLine("3) Minute to expire");
                Console.WriteLine("");
                Console.WriteLine("B) Buy by best bid price");
                Console.WriteLine("S) Sell by best ask price");
                Console.WriteLine("C) Cancel child order");
                Console.WriteLine("");
                Console.WriteLine("R) Return to main");

                switch (GetCh())
                {
                    case '1':
                        PlaceUnexecutableChildOrder();
                        break;

                    case '2':
                        PlaceUnexecutableChildOrder(fok: true);
                        break;

                    case '3':
                        PlaceUnexecutableChildOrder(mte: true);
                        break;

                    case 'B':
                        BuyBestBidPrice();
                        break;

                    case 'S':
                        SellBestAskPrice();
                        break;

                    case 'C':
                        CancelChildOrder();
                        break;

                    case 'R':
                        return;
                }
            }
        }

        static void PlaceUnexecutableChildOrder(bool fok = false, bool mte = false)
        {
            // Selling order too high price by best ask price. Order will be on order book but not execute.
            BfChildOrderRequest order = null;
            if (fok)
            {
                order = _orderFactory.CreateLimitPriceOrder(BfTradeSide.Sell, _market.MinimumOrderSize, _market.BestAskPrice + 50000.0m, timeInForce: BfTimeInForce.FOK);
            }
            else if (mte)
            {
                order = _orderFactory.CreateLimitPriceOrder(BfTradeSide.Sell, _market.MinimumOrderSize, _market.BestAskPrice + 50000.0m, minuteToExpire: TimeSpan.FromMinutes(1));
            }
            else
            {
                order = _orderFactory.CreateLimitPriceOrder(BfTradeSide.Sell, _market.MinimumOrderSize, _market.BestAskPrice + 50000.0m);
            }

            var transaction = _market.PlaceOrder(order).Result;
            if (transaction != null)
            {
                _childOrderTransaction = transaction;
                Console.WriteLine("Order accepted.");
            }
            else
            {
                Console.WriteLine("Order failed.");
            }
        }

        static void CancelChildOrder()
        {
            if (_childOrderTransaction == null || !_childOrderTransaction.IsCancelable())
            {
                return;
            }

            _childOrderTransaction.CancelOrder();
        }

        static void BuyBestBidPrice()
        {
            // Buy by best bid price within minimum size
            var order = _orderFactory.CreateLimitPriceOrder(BfTradeSide.Buy, _market.MinimumOrderSize, _market.BestBidPrice);
            var transaction = _market.PlaceOrder(order).Result;
            if (transaction != null)
            {
                _childOrderTransaction = transaction;
                Console.WriteLine("Order accepted.");
            }
            else
            {
                Console.WriteLine("Order failed.");
            }
        }

        static void SellBestAskPrice()
        {
            // Sell by best ask price within minimum size
            var order = _orderFactory.CreateLimitPriceOrder(BfTradeSide.Sell, _market.MinimumOrderSize, _market.BestAskPrice);
            var transaction = _market.PlaceOrder(order).Result;
            if (transaction != null)
            {
                _childOrderTransaction = transaction;
                Console.WriteLine("Order accepted.");
            }
            else
            {
                Console.WriteLine("Order failed.");
            }
        }

        static void OnChildOrderTransactionStateChanged(object sender, BfxChildOrderTransactionEventArgs args)
        {
            Console.WriteLine($"{args.Time.ToString("yyyy-MM-dd HH:mm:ss.fff")} Child order state changed to {args.Kind}");

            if (args.Kind == BfxOrderTransactionEventKind.OrderFailed)
            {
                Console.WriteLine(args.State.OrderFailedException.Message);
            }
        }

        static void OnChildOrderChanged(object sender, BfxChildOrderEventArgs args)
        {
            Console.WriteLine($"{_market.ServerTime.ToString("yyyy-MM-dd HH:mm:ss.fff")} Child order changed to {args.OrderState}");
        }
    }
}
