//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public interface IBfxOrderRequest
    {
        BfProductCode ProductCode { get; }
        BfOrderType OrderType { get; }
    }

    public class BfxSimpleOrderRequest : IBfxOrderRequest
    {
        internal BfChildOrderRequest ChildOrderRequest { get; private set; }
        internal BfParentOrderRequest ParentOrderRequest { get; private set; }

        public BfProductCode ProductCode { get; private set; }
        public BfOrderType OrderType => ChildOrderRequest?.ChildOrderType ?? ParentOrderRequest?.OrderMethod ?? BfOrderType.Unknown;

        public static BfxSimpleOrderRequest MarketPrice(BfProductCode productCode, BfTradeSide side, decimal size, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            return new BfxSimpleOrderRequest
            {
                ProductCode = productCode,
                ChildOrderRequest = BfChildOrderRequest.MarketPrice(productCode, side, size, minuteToExpire, timeInForce)
            };
        }

        public static BfxSimpleOrderRequest LimitPrice(BfProductCode productCode, BfTradeSide side, decimal size, decimal price, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            return new BfxSimpleOrderRequest
            {
                ProductCode = productCode,
                ChildOrderRequest = BfChildOrderRequest.LimitPrice(productCode, side, price, size, minuteToExpire, timeInForce)
            };
        }

        public static BfxSimpleOrderRequest Stop(BfProductCode productCode, BfTradeSide side, decimal size, decimal triggerPrice, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            return new BfxSimpleOrderRequest
            {
                ProductCode = productCode,
                ParentOrderRequest = BfParentOrderRequest.Stop(productCode, side, size, triggerPrice, minuteToExpire, timeInForce),
            };
        }

        public static BfxSimpleOrderRequest StopLimit(BfProductCode productCode, BfTradeSide side, decimal size, decimal price, decimal triggerPrice, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            return new BfxSimpleOrderRequest
            {
                ProductCode = productCode,
                ParentOrderRequest = BfParentOrderRequest.StopLimit(productCode, side, size, price, triggerPrice, minuteToExpire, timeInForce),
            };
        }

        public static BfxSimpleOrderRequest Trail(BfProductCode productCode, BfTradeSide side, decimal size, decimal offset, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            if (size > offset)
            {
                throw new ArgumentException();
            }

            return new BfxSimpleOrderRequest
            {
                ProductCode = productCode,
                ParentOrderRequest = BfParentOrderRequest.Trail(productCode, side, size, offset, minuteToExpire, timeInForce)
            };
        }

        void CheckChildOrderRequestValid(BfChildOrderRequest request)
        {
            if (request.ChildOrderType != BfOrderType.Market && request.Size > request.Price)
            {
                throw new ArgumentException();
            }
        }
    }

    public class BfxConditionalOrderRequest : IBfxOrderRequest
    {
        internal BfParentOrderRequest ParentOrderRequest { get; private set; }

        public BfProductCode ProductCode { get; private set; }
        public BfOrderType OrderType => ParentOrderRequest?.OrderMethod ?? BfOrderType.Unknown;

        public static BfxConditionalOrderRequest IFD(BfxSimpleOrderRequest first, BfxSimpleOrderRequest second, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            return new BfxConditionalOrderRequest
            {
                ProductCode = first.ProductCode,
                ParentOrderRequest = BfParentOrderRequest.IFD
                (
                    first.ChildOrderRequest?.ToParameter() ?? first.ParentOrderRequest?.ToParameter(),
                    second.ChildOrderRequest?.ToParameter() ?? second.ParentOrderRequest?.ToParameter(),
                    minuteToExpire,
                    timeInForce
                )
            };
        }

        public static BfxConditionalOrderRequest OCO(BfxSimpleOrderRequest first, BfxSimpleOrderRequest second, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            return new BfxConditionalOrderRequest
            {
                ProductCode = first.ProductCode,
                ParentOrderRequest = BfParentOrderRequest.OCO
                (
                    first.ChildOrderRequest?.ToParameter() ?? first.ParentOrderRequest?.ToParameter(),
                    second.ChildOrderRequest?.ToParameter() ?? second.ParentOrderRequest?.ToParameter(),
                    minuteToExpire,
                    timeInForce
                )
            };
        }

        public static BfxConditionalOrderRequest IFDOCO(BfxSimpleOrderRequest ifdone, BfxSimpleOrderRequest ocoFirst, BfxSimpleOrderRequest ocoSecond, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            return new BfxConditionalOrderRequest
            {
                ProductCode = ifdone.ProductCode,
                ParentOrderRequest = BfParentOrderRequest.IFDOCO
                (
                    ifdone.ChildOrderRequest?.ToParameter() ?? ifdone.ParentOrderRequest?.ToParameter(),
                    ocoFirst.ChildOrderRequest?.ToParameter() ?? ocoFirst.ParentOrderRequest?.ToParameter(),
                    ocoSecond.ChildOrderRequest?.ToParameter() ?? ocoSecond.ParentOrderRequest?.ToParameter(),
                    minuteToExpire,
                    timeInForce
                )
            };
        }
    }
}
