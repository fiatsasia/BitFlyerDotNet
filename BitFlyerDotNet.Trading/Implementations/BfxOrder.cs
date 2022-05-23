//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Collections.Generic;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public abstract class BfxOrder : IBfxOrder
    {
        public string ProductCode { get; protected set; }
        public BfOrderType OrderType { get; protected set; }

        public BfTradeSide? Side { get; protected set; }
        public decimal? OrderPrice { get; protected set; }
        public decimal? OrderSize { get; protected set; }
        public decimal? TriggerPrice { get; protected set; }
        public decimal? TrailOffset { get; protected set; }

        public abstract string AcceptanceId { get; protected set; }
        public abstract string OrderId { get; protected set; }

        public int MinuteToExpire { get; internal set; }
        public BfTimeInForce TimeInForce { get; internal set; }

        public DateTime? OrderDate { get; protected set; }
        public DateTime? ExpireDate { get; internal set; }

        public decimal? ExecutedSize { get; protected set; }
        public decimal? ExecutedPrice { get; protected set; }
        public decimal? Commission { get; protected set; }

        static IReadOnlyList<IBfxExecution> EmptyExecutions = new List<IBfxExecution>();
        public virtual IReadOnlyList<IBfxExecution> Executions => EmptyExecutions;
        public decimal? SfdCollectedAmount => Executions.Sum(e => e.SfdCollectedAmount);

        public BfxOrderState State { get; protected set; } = BfxOrderState.Outstanding;
        public DateTime? LastUpdatedTime { get; protected set; }

        static IBfxOrder[] EmptyChildren = new IBfxOrder[0];
        public virtual IBfxOrder[] Children => EmptyChildren;

        public string OrderFailedReason { get; protected set; } = string.Empty;

        internal abstract void ApplyParameters(string productCode, int minutesToExpire, BfTimeInForce timeInForce);

        #region Request Builders
        public static IBfxOrder Market(string productCode, BfTradeSide side, decimal size)
        {
            var request = BfChildOrderRequest.Market(productCode, side, size, 0, BfTimeInForce.NotSpecified);
            var order = new BfxChildOrder(request);
            return order;
        }

        public static IBfxOrder Limit(string productCode, BfTradeSide side, decimal price, decimal size)
        {
            var request = BfChildOrderRequest.Limit(productCode, side, price, size, 0, BfTimeInForce.NotSpecified);
            var order = new BfxChildOrder(request);
            return order;
        }

        public static IBfxOrder Stop(string productCode, BfTradeSide side, decimal triggerPrice, decimal size)
        {
            var request = BfParentOrderRequest.Stop(productCode, side, triggerPrice, size, 0, BfTimeInForce.NotSpecified);
            var order = new BfxParentOrder(request);
            return order;
        }

        public static IBfxOrder StopLimit(string productCode, BfTradeSide side, decimal triggerPrice, decimal orderPrice, decimal size)
        {
            var request = BfParentOrderRequest.StopLimit(productCode, side, triggerPrice, orderPrice, size, 0, BfTimeInForce.NotSpecified);
            var order = new BfxParentOrder(request);
            return order;
        }

        public static IBfxOrder Trailing(string productCode, BfTradeSide side, decimal trailingOffset, decimal size)
        {
            var request = BfParentOrderRequest.Trail(productCode, side, trailingOffset, size, 0, BfTimeInForce.NotSpecified);
            var order = new BfxParentOrder(request);
            return order;
        }

        public static IBfxOrder IFD(IBfxOrder ifOrder, IBfxOrder doneOrder)
        {
            var request = BfParentOrderRequest.IFD(
                GetParentOrderRequestParameter(ifOrder),
                GetParentOrderRequestParameter(doneOrder),
                0, BfTimeInForce.NotSpecified
            );
            return new BfxParentOrder(request);
        }

        public static IBfxOrder OCO(IBfxOrder firstOrder, IBfxOrder secondOrder)
        {
            var request = BfParentOrderRequest.OCO(
                GetParentOrderRequestParameter(firstOrder),
                GetParentOrderRequestParameter(secondOrder),
                0, BfTimeInForce.NotSpecified
            );
            return new BfxParentOrder(request);
        }

        public static IBfxOrder IFDOCO(IBfxOrder ifOrder, IBfxOrder firstOrder, IBfxOrder secondOrder)
        {
            var request = BfParentOrderRequest.IFDOCO(
                GetParentOrderRequestParameter(ifOrder),
                GetParentOrderRequestParameter(firstOrder),
                GetParentOrderRequestParameter(secondOrder),
                0, BfTimeInForce.NotSpecified
            );
            return new BfxParentOrder(request);
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
        #endregion Request Builders
    }
}
