//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public partial class TradingAccount : ITradingAccount
    {
        public IChildOrderTransaction CreateMarketPriceOrder(BfTradeSide side, double size)
        {
            return new ChildOrderTransaction(this, BfOrderType.Market, side, size);
        }

        public IChildOrderTransaction CreateLimitPriceOrder(BfTradeSide side, double size, double price)
        {
            return new ChildOrderTransaction(this, BfOrderType.Limit, side, size, price);
        }

        public IParentOrderTransaction CreateStopOrder(BfTradeSide side, double size, double stopTriggerPrice)
        {
            return new ParentOrderTransaction(this, BfOrderType.Simple, new IChildOrder[] { new StopOrder(this.ProductCode, side, size, stopTriggerPrice) });
        }

        public IParentOrderTransaction CreateStopLimitOrder(BfTradeSide side, double size, double price, double stopTriggerPrice)
        {
            return new ParentOrderTransaction(this, BfOrderType.Simple, new IChildOrder[] { new StopLimitOrder(this.ProductCode, side, size, price, stopTriggerPrice) });
        }

        public IParentOrderTransaction CreateTrailOrder(BfTradeSide side, double size, double trailingStopPriceOffset)
        {
            return new ParentOrderTransaction(this, BfOrderType.Simple, new IChildOrder[] { new TrailingStopOrder(this.ProductCode, side, size, trailingStopPriceOffset) });
        }

        public IParentOrderTransaction CreateIFD(IChildOrder first, IChildOrder second)
        {
            return new ParentOrderTransaction(this, BfOrderType.IFD, new IChildOrder[] { first, second });
        }

        public IParentOrderTransaction CreateOCO(IChildOrder first, IChildOrder second)
        {
            return new ParentOrderTransaction(this, BfOrderType.OCO, new IChildOrder[] { first, second });
        }

        public IParentOrderTransaction CreateIFDOCO(IChildOrder ifdone, IChildOrder ocoFirst, IChildOrder ocoSecond)
        {
            return new ParentOrderTransaction(this, BfOrderType.IFDOCO, new IChildOrder[] { ifdone, ocoFirst, ocoSecond });
        }
    }
}
