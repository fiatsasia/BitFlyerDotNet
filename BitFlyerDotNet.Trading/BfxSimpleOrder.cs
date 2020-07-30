//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Text;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxSimpleOrder
    {
        public static IBfxSimpleOrder MarketPrice(BfTradeSide side, decimal size)
        {
            throw new NotImplementedException();
        }

        public static IBfxSimpleOrder LimitPrice(BfxMarket market, BfTradeSide side, decimal price, decimal size, TimeSpan periodToExpire, BfTimeInForce timeInForce)
        {
            var request = BfChildOrderRequest.LimitPrice(market.ProductCode, side, price, size, Convert.ToInt32(periodToExpire.TotalMinutes), timeInForce);
            var order = new BfxChildOrder(request);
            return order;
        }
        public static IBfxSimpleOrder LimitPrice(BfxMarket market, BfTradeSide side, decimal price, decimal size)
        {
            return LimitPrice(market, side, price, size, TimeSpan.Zero, BfTimeInForce.NotSpecified);
        }

        public static IBfxSimpleOrder StopLoss(BfTradeSide side, decimal triggerPrice, decimal size)
        {
            throw new NotImplementedException();
        }

        public static IBfxSimpleOrder StopLimit(BfTradeSide side, decimal triggerPrice, decimal orderPrice, decimal size)
        {
            throw new NotImplementedException();
        }

        public static IBfxSimpleOrder TrailingStop(BfTradeSide side, decimal trailingOffset, decimal size)
        {
            throw new NotImplementedException();
        }
    }
}
