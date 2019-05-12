//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public static class TradeOrderFactory
    {
        public static IChildOrderTransaction CreateMarketPriceOrder(TradingAccount account, BfTradeSide side, decimal size)
        {
            return new ChildOrderTransaction(account, BfOrderType.Market, side, size);
        }

        public static IChildOrderTransaction CreateLimitPriceOrder(TradingAccount account, BfTradeSide side, decimal size, decimal price)
        {
            return new ChildOrderTransaction(account, BfOrderType.Limit, side, size, price);
        }

        public static IParentOrderTransaction CreateStopOrder(TradingAccount account, BfTradeSide side, decimal size, decimal stopTriggerPrice)
        {
            return new ParentOrderTransaction(account, BfOrderType.Simple, new IChildOrder[] { new StopOrder(account.ProductCode, side, size, stopTriggerPrice) });
        }

        public static IParentOrderTransaction CreateStopLimitOrder(TradingAccount account, BfTradeSide side, decimal size, decimal price, decimal stopTriggerPrice)
        {
            return new ParentOrderTransaction(account, BfOrderType.Simple, new IChildOrder[] { new StopLimitOrder(account.ProductCode, side, size, price, stopTriggerPrice) });
        }

        public static IParentOrderTransaction CreateTrailOrder(TradingAccount account, BfTradeSide side, decimal size, decimal trailingStopPriceOffset)
        {
            return new ParentOrderTransaction(account, BfOrderType.Simple, new IChildOrder[] { new TrailingStopOrder(account.ProductCode, side, size, trailingStopPriceOffset) });
        }

        public static IParentOrderTransaction CreateIFD(TradingAccount account, IChildOrder first, IChildOrder second)
        {
            return new ParentOrderTransaction(account, BfOrderType.IFD, new IChildOrder[] { first, second });
        }

        public static IParentOrderTransaction CreateOCO(TradingAccount account, IChildOrder first, IChildOrder second)
        {
            return new ParentOrderTransaction(account, BfOrderType.OCO, new IChildOrder[] { first, second });
        }

        public static IParentOrderTransaction CreateIFDOCO(TradingAccount account, IChildOrder ifdone, IChildOrder ocoFirst, IChildOrder ocoSecond)
        {
            return new ParentOrderTransaction(account, BfOrderType.IFDOCO, new IChildOrder[] { ifdone, ocoFirst, ocoSecond });
        }
    }
}
