//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using BitFlyerDotNet.LightningApi;

namespace OrderApiSample
{
    partial class Program
    {
        static string _parentOrderAcceptanceId;

        static void ParentOrderMain()
        {
            while (true)
            {
                Console.WriteLine("======== Parent order operations");
                Console.WriteLine("1) Stop          2) Stop Limit");
                Console.WriteLine("3) Trail         4) IFD");
                Console.WriteLine("5) OCO");
                Console.WriteLine("6) IFD (mitnutes to expire)");
                Console.WriteLine("7) IFD (FOK)");
                Console.WriteLine("");
                Console.WriteLine("C) Cancel parent order");
                Console.WriteLine("");
                Console.WriteLine("R) Return to main");

                switch (GetCh())
                {
                    case '1':
                        PlaceUnexecutableParentOrder(BfOrderType.Stop);
                        break;

                    case '2':
                        PlaceUnexecutableParentOrder(BfOrderType.StopLimit);
                        break;

                    case '3':
                        PlaceUnexecutableParentOrder(BfOrderType.Trail);
                        break;

                    case '4':
                        PlaceUnexecutableParentOrder(BfOrderType.IFD);
                        break;

                    case '5':
                        PlaceUnexecutableParentOrder(BfOrderType.OCO);
                        break;

                    case '6':
                        PlaceUnexecutableParentOrder(BfOrderType.IFD, mte: true);
                        break;

                    case '7':
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
            var childOrder1 = BfParentOrderRequestParameter.LimitPrice(ProductCode, BfTradeSide.Sell, _orderSize, _ticker.BestAsk + 50000.0m);
            var childOrder2 = BfParentOrderRequestParameter.LimitPrice(ProductCode, BfTradeSide.Buy, _orderSize, _ticker.BestBid - 50000.0m);
            BfParentOrderRequest request;
            switch (orderType)
            {
                case BfOrderType.Stop:
                    request = BfParentOrderRequest.Stop(ProductCode, BfTradeSide.Buy, _orderSize, _ticker.BestAsk + 50000m);
                    break;

                case BfOrderType.StopLimit:
                    request = BfParentOrderRequest.StopLimit(ProductCode, BfTradeSide.Buy, _orderSize, _ticker.BestAsk + 40000m, _ticker.BestAsk + 50000m);
                    break;

                case BfOrderType.Trail:
                    request = BfParentOrderRequest.Trail(ProductCode, BfTradeSide.Buy, _orderSize, 50000m);
                    break;

                case BfOrderType.IFD:
                    request = BfParentOrderRequest.IFD(
                        childOrder1,
                        childOrder2,
                        mte ? _minuteToExpire : 0,
                        fok ? BfTimeInForce.FOK : BfTimeInForce.NotSpecified
                    );
                    break;

                case BfOrderType.OCO:
                    request = BfParentOrderRequest.OCO(
                        childOrder1,
                        childOrder2,
                        mte ? _minuteToExpire : 0,
                        fok ? BfTimeInForce.FOK : BfTimeInForce.NotSpecified
                    );
                    break;

                default:
                    throw new AggregateException();
            }

            var resp = _client.SendParentOrder(request);
            if (!resp.IsErrorOrEmpty)
            {
                _parentOrderAcceptanceId = resp.GetMessage().ParentOrderAcceptanceId;
            }
        }

        static void CancelParentOrder()
        {
            var resp = _client.CancelParentOrder(ProductCode, parentOrderAcceptanceId: _parentOrderAcceptanceId);
            if (!resp.IsErrorOrEmpty)
            {
                // Cancel order sent
            }
        }
    }
}
