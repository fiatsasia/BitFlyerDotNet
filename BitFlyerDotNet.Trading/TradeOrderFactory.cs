//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public partial class TradeAccount : ITradeAccount
    {
        public IBfTradeOrder CreateMarketPriceOrder(BfTradeSide side, double size)
        {
            return new ChildOrder(this, BfOrderType.Market, side, size);
        }

        public IBfTradeOrder CreateLimitPriceOrder(BfTradeSide side, double size, double price)
        {
            return new ChildOrder(this, BfOrderType.Limit, side, size, price);
        }

        public IBfTradeOrder CreateStopOrder(BfTradeSide side, double size, double triggerPrice)
        {
            return new SimpleOrder(this, BfOrderType.Stop, side, size, double.NaN, triggerPrice: triggerPrice);
        }

        public IBfTradeOrder CreateStopLimitOrder(BfTradeSide side, double size, double price, double triggerPrice)
        {
            return new SimpleOrder(this, BfOrderType.StopLimit, side, size, price, triggerPrice: triggerPrice);
        }

        public IBfTradeOrder CreateTrailOrder(BfTradeSide side, double size, double limitOffset)
        {
            return new SimpleOrder(this, BfOrderType.Trail, side, size, limitOffset: limitOffset);
        }

        public IBfTradeOrder CreateIFD(IBfTradeOrder first, IBfTradeOrder second)
        {
            return new ParentOrder(this, BfParentOrderMethod.IFD, new IBfTradeOrder[] { first, second });
        }

        public IBfTradeOrder CreateOCO(IBfTradeOrder first, IBfTradeOrder second)
        {
            return new ParentOrder(this, BfParentOrderMethod.OCO, new IBfTradeOrder[] { first, second });
        }

        public IBfTradeOrder CreateIFDOCO(IBfTradeOrder ifdone, IBfTradeOrder ocoFirst, IBfTradeOrder ocoSecond)
        {
            return new ParentOrder(this, BfParentOrderMethod.IFDOCO, new IBfTradeOrder[] { ifdone, ocoFirst, ocoSecond });
        }
    }
}
