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
        static BfxParentOrderTransaction _parentOrderTransaction;

        static void ParentOrderMain()
        {
            while (true)
            {
                Console.WriteLine("======== Parent order operations");
                Console.WriteLine("1) Place unexecutable parent order (IFD)");
                Console.WriteLine("2) Place unexecutable parent order (OCO)");
                Console.WriteLine("3) Place unexecutable parent order (mitnutes to expire)");
                Console.WriteLine("4) Place unexecutable parent order (FOK)");
                Console.WriteLine("");
                Console.WriteLine("C) Cancel parent order");
                Console.WriteLine("");
                Console.WriteLine("R) Return to main");

                switch (GetCh())
                {
                    case '1':
                        PlaceUnexecutableParentOrder(BfOrderType.IFD);
                        break;

                    case '2':
                        PlaceUnexecutableParentOrder(BfOrderType.OCO);
                        break;

                    case '3':
                        PlaceUnexecutableParentOrder(BfOrderType.IFD, mte: true);
                        break;

                    case '4':
                        PlaceUnexecutableParentOrder(BfOrderType.IFD, fok: true);
                        break;

                    case 'C':
                        CancelParentOrder();
                        break;

                    case 'R':
                        return;
                }
            }
        }

        static void PlaceUnexecutableParentOrder(BfOrderType orderType, bool mte = false, bool fok = false)
        {
            var childOrder1 = _orderFactory.CreateLimitPriceOrder(BfTradeSide.Sell, _market.MinimumOrderSize, _market.BestAskPrice + 50000.0m);
            var childOrder2 = _orderFactory.CreateLimitPriceOrder(BfTradeSide.Buy, _market.MinimumOrderSize, _market.BestBidPrice - 50000.0m);
            BfParentOrderRequest request;
            switch (orderType)
            {
                case BfOrderType.IFD:
                    request = _orderFactory.CreateIFD(
                        childOrder1.ToParameter(),
                        childOrder2.ToParameter(),
                        mte ? TimeSpan.FromMinutes(1) : default(TimeSpan),
                        fok ? BfTimeInForce.FOK : BfTimeInForce.NotSpecified
                    );
                    break;

                case BfOrderType.OCO:
                    request = _orderFactory.CreateOCO(
                        childOrder1.ToParameter(),
                        childOrder2.ToParameter(),
                        mte ? TimeSpan.FromMinutes(1) : default(TimeSpan),
                        fok ? BfTimeInForce.FOK : BfTimeInForce.NotSpecified
                    );
                    break;

                default:
                    throw new AggregateException();
            }

            var transaction = _market.PlaceOrder(request).Result;
            if (transaction != null)
            {
                _parentOrderTransaction = transaction;
                Console.WriteLine("Order accepted.");
            }
            else
            {
                Console.WriteLine("Order failed.");
            }
        }

        static void PlaceParentOrder()
        {
        }

        static void CancelParentOrder()
        {
            if (_parentOrderTransaction == null || !_parentOrderTransaction.IsCancelable())
            {
                return;
            }

            _parentOrderTransaction.CancelOrder();
        }

        static void OnParentOrderTransactionStateChanged(object sender, BfxParentOrderTransactionEventArgs args)
        {
            Console.WriteLine($"{args.Time.ToString("yyyy-MM-dd HH:mm:ss.fff")} Parent order state changed to {args.Kind}");

            if (args.Kind == BfxOrderTransactionEventKind.OrderFailed)
            {
                Console.WriteLine(args.State.OrderFailedException.Message);
            }
        }

        static void OnParentOrderChanged(object sender, BfxParentOrderEventArgs args)
        {
            Console.WriteLine($"{_market.ServerTime.ToString("yyyy-MM-dd HH:mm:ss.fff")} Parent order changed to {args.ParentOrderState}");
        }
    }
}
