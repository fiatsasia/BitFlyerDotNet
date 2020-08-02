//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxOrder
    {
        public static IBfxOrder MarketPrice(BfTradeSide side, decimal size, TimeSpan periodToExpire, BfTimeInForce timeInForce)
        {
            var request = BfChildOrderRequest.MarketPrice(BfProductCode.Unknown, side, size, Convert.ToInt32(periodToExpire.TotalMinutes), timeInForce);
            var order = new BfxChildOrder(request);
            return order;
        }

        public static IBfxOrder MarketPrice(BfTradeSide side, decimal size)
        {
            return MarketPrice(side, size, TimeSpan.Zero, BfTimeInForce.NotSpecified);
        }

        public static IBfxOrder LimitPrice(BfTradeSide side, decimal price, decimal size, TimeSpan periodToExpire, BfTimeInForce timeInForce)
        {
            var request = BfChildOrderRequest.LimitPrice(BfProductCode.Unknown, side, price, size, Convert.ToInt32(periodToExpire.TotalMinutes), timeInForce);
            var order = new BfxChildOrder(request);
            return order;
        }

        public static IBfxOrder LimitPrice(BfTradeSide side, decimal price, decimal size)
        {
            return LimitPrice(side, price, size, TimeSpan.Zero, BfTimeInForce.NotSpecified);
        }

        public static IBfxOrder StopLoss(BfTradeSide side, decimal triggerPrice, decimal size, TimeSpan periodToExpire, BfTimeInForce timeInForce)
        {
            var request = BfParentOrderRequest.Stop(BfProductCode.Unknown, side, triggerPrice, size, Convert.ToInt32(periodToExpire.TotalMinutes), timeInForce);
            var order = new BfxParentOrder(request);
            return order;
        }

        public static IBfxOrder StopLoss(BfTradeSide side, decimal triggerPrice, decimal size)
        {
            return StopLoss(side, triggerPrice, size, TimeSpan.Zero, BfTimeInForce.NotSpecified);
        }

        public static IBfxOrder StopLimit(BfTradeSide side, decimal triggerPrice, decimal orderPrice, decimal size, TimeSpan periodToExpire, BfTimeInForce timeInForce)
        {
            var request = BfParentOrderRequest.StopLimit(BfProductCode.Unknown, side, triggerPrice, orderPrice, size, Convert.ToInt32(periodToExpire.TotalMinutes), timeInForce);
            var order = new BfxParentOrder(request);
            return order;
        }

        public static IBfxOrder StopLimit(BfTradeSide side, decimal triggerPrice, decimal orderPrice, decimal size)
        {
            return StopLimit(side, triggerPrice, orderPrice, size, TimeSpan.Zero, BfTimeInForce.NotSpecified);
        }

        public static IBfxOrder TrailingStop(BfTradeSide side, decimal trailingOffset, decimal size, TimeSpan periodToExpire, BfTimeInForce timeInForce)
        {
            var request = BfParentOrderRequest.Trail(BfProductCode.Unknown, side, trailingOffset, size, Convert.ToInt32(periodToExpire.TotalMinutes), timeInForce);
            var order = new BfxParentOrder(request);
            return order;
        }

        public static IBfxOrder TrailingStop(BfTradeSide side, decimal trailingOffset, decimal size)
        {
            return TrailingStop(side, trailingOffset, size, TimeSpan.Zero, BfTimeInForce.NotSpecified);
        }

        public static IBfxOrder IFD(IBfxOrder ifOrder, IBfxOrder doneOrder, TimeSpan periodToExpire, BfTimeInForce timeInForce)
        {
            var request = BfParentOrderRequest.IFD(
                GetParentOrderRequestParameter(ifOrder),
                GetParentOrderRequestParameter(doneOrder),
                Convert.ToInt32(periodToExpire.TotalMinutes), timeInForce
            );
            return new BfxParentOrder(request);
        }

        public static IBfxOrder IFD(IBfxOrder ifOrder, IBfxOrder doneOrder)
        {
            return IFD(ifOrder, doneOrder, TimeSpan.Zero, BfTimeInForce.NotSpecified);
        }

        public static IBfxOrder OCO(IBfxOrder firstOrder, IBfxOrder secondOrder, TimeSpan periodToExpire, BfTimeInForce timeInForce)
        {
            var request = BfParentOrderRequest.OCO(
                GetParentOrderRequestParameter(firstOrder),
                GetParentOrderRequestParameter(secondOrder),
                Convert.ToInt32(periodToExpire.TotalMinutes), timeInForce
            );
            return new BfxParentOrder(request);
        }

        public static IBfxOrder OCO(IBfxOrder firstOrder, IBfxOrder secondOrder)
        {
            return OCO(firstOrder, secondOrder, TimeSpan.Zero, BfTimeInForce.NotSpecified);
        }

        public static IBfxOrder IFDOCO(IBfxOrder ifOrder, IBfxOrder firstOrder, IBfxOrder secondOrder, TimeSpan periodToExpire, BfTimeInForce timeInForce)
        {
            var request = BfParentOrderRequest.IFDOCO(
                GetParentOrderRequestParameter(ifOrder),
                GetParentOrderRequestParameter(firstOrder),
                GetParentOrderRequestParameter(secondOrder),
                Convert.ToInt32(periodToExpire.TotalMinutes), timeInForce
            );
            return new BfxParentOrder(request);
        }

        public static IBfxOrder IFDOCO(IBfxOrder ifOrder, IBfxOrder firstOrder, IBfxOrder secondOrder)
        {
            return IFDOCO(ifOrder, firstOrder, secondOrder, TimeSpan.Zero, BfTimeInForce.NotSpecified);
        }

        static BfParentOrderRequestParameter GetParentOrderRequestParameter(IBfxOrder order)
        {
            if (order is BfxChildOrder cif && cif.Request != null)
            {
                return cif.Request.ToParameter();
            }
            else if (order is BfxParentOrder pif && pif.Request != null && pif.Request.OrderMethod == BfOrderType.Simple)
            {
                return pif.Request.Parameters[0];
            }
            else
            {
                throw new ArgumentException();
            }
        }
    }
}
