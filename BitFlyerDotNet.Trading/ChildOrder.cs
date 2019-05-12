//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class ChildOrder : IChildOrder
    {
        public virtual BfProductCode ProductCode { get; set; }
        public virtual BfOrderType OrderType { get; set; }
        public virtual BfTradeSide Side { get; set; }
        public virtual decimal OrderSize { get; set; }
        public virtual decimal OrderPrice { get; set; }
        public virtual decimal StopTriggerPrice { get; set; }
        public virtual decimal TrailingStopPriceOffset { get; set; }

        public ChildOrder()
        {
        }

        public ChildOrder(
            BfProductCode productCode,
            BfOrderType orderType,
            BfTradeSide side,
            decimal size,
            decimal price = decimal.Zero,
            decimal stopTriggerPrice = decimal.Zero,
            decimal trailingStopPriceOffset = decimal.Zero
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
            OrderPrice = order.Price;
        }

        public ChildOrder(BfProductCode productCode, BfChildOrderElement order)
        {
            ProductCode = productCode; // need parsed enum for future aliases
            OrderType = order.ConditionType;
            Side = order.Side;
            OrderSize = order.Size;
            OrderPrice = order.Price;
            StopTriggerPrice = order.TriggerPrice;
            TrailingStopPriceOffset = order.Offset;
        }
    }

    public class LimitPriceOrder : ChildOrder
    {
        public LimitPriceOrder(BfProductCode productCode, BfTradeSide side, decimal size, decimal price)
            : base(productCode, BfOrderType.Limit, side, size, price: price)
        {
        }
    }

    public class MarketPriceOrder : ChildOrder
    {
        public MarketPriceOrder(BfProductCode productCode, BfTradeSide side, decimal size)
            : base(productCode, BfOrderType.Market, side, size)
        {
        }
    }

    public class StopOrder : ChildOrder
    {
        public StopOrder(BfProductCode productCode, BfTradeSide side, decimal size, decimal stopTriggerPrice)
            : base(productCode, BfOrderType.Stop, side, size, stopTriggerPrice: stopTriggerPrice)
        {
        }
    }

    public class StopLimitOrder : ChildOrder
    {
        public StopLimitOrder(BfProductCode productCode, BfTradeSide side, decimal size, decimal price, decimal stopTriggerPrice)
            : base(productCode, BfOrderType.StopLimit, side, size, price: price, stopTriggerPrice: stopTriggerPrice)
        {
        }
    }

    public class TrailingStopOrder : ChildOrder
    {
        public TrailingStopOrder(BfProductCode productCode, BfTradeSide side, decimal size, decimal trailingStopPriceOffset)
            : base(productCode, BfOrderType.Trail, side, size, trailingStopPriceOffset: trailingStopPriceOffset)
        {
        }
    }
}
