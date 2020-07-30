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
        static string _childOrderAcceptanceId;

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

        // Selling order too high price by best ask price. Order will be on order book but not execute.
        static void PlaceUnexecutableChildOrder(bool fok = false, bool mte = false)
        {
            BfChildOrderRequest request;
            if (fok)
            {
                request = BfChildOrderRequest.LimitPrice(ProductCode, BfTradeSide.Sell, _orderSize, _ticker.BestAsk + 50000m, timeInForce: BfTimeInForce.FOK);
            }
            else if (mte)
            {
                request = BfChildOrderRequest.LimitPrice(ProductCode, BfTradeSide.Sell, _orderSize, _ticker.BestAsk + 50000.0m, minuteToExpire: _minuteToExpire);
            }
            else
            {
                request = BfChildOrderRequest.LimitPrice(ProductCode, BfTradeSide.Sell, _orderSize, _ticker.BestAsk + 50000m);
            }

            var resp = _client.SendChildOrder(request);
            if (!resp.IsErrorOrEmpty)
            {
                _childOrderAcceptanceId = resp.GetMessage().ChildOrderAcceptanceId;
            }
        }

        static void CancelChildOrder()
        {
            var resp = _client.CancelChildOrder(ProductCode, childOrderAcceptanceId: _childOrderAcceptanceId);
            if (!resp.IsErrorOrEmpty)
            {
                // Cancel order sent
            }
        }

        // Buy by best bid price within minimum size
        static void BuyBestBidPrice()
        {
            var request = BfChildOrderRequest.LimitPrice(ProductCode, BfTradeSide.Buy, _orderSize, _ticker.BestBid);
            var resp = _client.SendChildOrder(request);
            if (!resp.IsErrorOrEmpty)
            {
                _childOrderAcceptanceId = resp.GetMessage().ChildOrderAcceptanceId;
            }
        }

        // Sell by best ask price within minimum size
        static void SellBestAskPrice()
        {
            var request = BfChildOrderRequest.LimitPrice(ProductCode, BfTradeSide.Sell, _orderSize, _ticker.BestAsk);
            var resp = _client.SendChildOrder(request);
            if (!resp.IsErrorOrEmpty)
            {
                _childOrderAcceptanceId = resp.GetMessage().ChildOrderAcceptanceId;
            }
        }
    }
}
