//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class ChildOrder : IChildOrder
    {
        public virtual BfProductCode ProductCode { get; set; }
        public virtual BfOrderType OrderType { get; set; }
        public virtual BfTradeSide Side { get; set; }
        public virtual double OrderSize { get; set; }
        public virtual double OrderPrice { get; set; }
        public virtual double StopTriggerPrice { get; set; }
        public virtual double TrailingStopPriceOffset { get; set; }

        public ChildOrder()
        {
        }

        public ChildOrder(
            BfProductCode productCode,
            BfOrderType orderType,
            BfTradeSide side,
            double size,
            double price = double.NaN,
            double stopTriggerPrice = double.NaN,
            double trailingStopPriceOffset = double.NaN
        )
        {
            ProductCode = productCode;
            OrderType = orderType;
            Side = side;
            OrderSize = size;
            OrderPrice = price;
            StopTriggerPrice = stopTriggerPrice;
            TrailingStopPriceOffset = trailingStopPriceOffset;
        }

        public ChildOrder(BfProductCode productCode, BfChildOrder order)
        {
            ProductCode = productCode; // need parsed enum for future aliases
            OrderType = order.ChildOrderType;
            Side = order.Side;
            OrderSize = order.Size;
            OrderPrice = (order.Price == 0.0) ? double.NaN : order.Price;
        }

        public ChildOrder(BfProductCode productCode, BfChildOrderElement order)
        {
            ProductCode = productCode; // need parsed enum for future aliases
            OrderType = order.ConditionType;
            Side = order.Side;
            OrderSize = order.Size;
            OrderPrice = (order.Price == 0.0) ? double.NaN : order.Price;
            StopTriggerPrice = (order.TriggerPrice == 0.0) ? double.NaN : order.TriggerPrice;
            TrailingStopPriceOffset = (order.Offset == 0.0) ? double.NaN : order.Offset;
        }
    }

    public class LimitPriceOrder : ChildOrder
    {
        public LimitPriceOrder(BfProductCode productCode, BfTradeSide side, double size, double price)
            : base(productCode, BfOrderType.Limit, side, size, price: price)
        {
        }
    }

    public class MarketPriceOrder : ChildOrder
    {
        public MarketPriceOrder(BfProductCode productCode, BfTradeSide side, double size)
            : base(productCode, BfOrderType.Market, side, size)
        {
        }
    }

    public class StopOrder : ChildOrder
    {
        public StopOrder(BfProductCode productCode, BfTradeSide side, double size, double stopTriggerPrice)
            : base(productCode, BfOrderType.Stop, side, size, stopTriggerPrice: stopTriggerPrice)
        {
        }
    }

    public class StopLimitOrder : ChildOrder
    {
        public StopLimitOrder(BfProductCode productCode, BfTradeSide side, double size, double price, double stopTriggerPrice)
            : base(productCode, BfOrderType.StopLimit, side, size, price: price, stopTriggerPrice: stopTriggerPrice)
        {
        }
    }

    public class TrailingStopOrder : ChildOrder
    {
        public TrailingStopOrder(BfProductCode productCode, BfTradeSide side, double size, double trailingStopPriceOffset)
            : base(productCode, BfOrderType.Trail, side, size, trailingStopPriceOffset: trailingStopPriceOffset)
        {
        }
    }
}
