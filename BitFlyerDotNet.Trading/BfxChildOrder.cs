//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxChildOrder : IBfxChildOrder
    {
        // Request fields
        public BfProductCode ProductCode { get; private set; }
        public BfOrderType OrderType { get; }
        public BfTradeSide? Side { get; }
        public decimal? OrderPrice { get; } // Missing if market price order
        public decimal? OrderSize { get; }
        public decimal? TriggerPrice { get; }
        public decimal? TrailOffset { get; }
        public int MinuteToExpire { get; }
        public BfTimeInForce TimeInForce { get; }

        // Response fields
        public string? ChildOrderAcceptanceId { get; private set; }

        // Confirmed fields
        public string? ChildOrderId { get; private set; }
        public DateTime? OrderDate { get; private set; }
        public DateTime? ExpireDate { get; private set; }

        // Execution fields
        public decimal? ExecutedSize { get; private set; }
        public decimal? ExecutedPrice { get; private set; }
        public decimal? Commission { get; private set; }
        public decimal? SfdCollectedAmount => _executions.Sum(e => e.SfdCollectedAmount);
        public IBfxExecution[] Executions => _executions.ToArray();

        // Simple order fields
        public string? AcceptanceId => ChildOrderAcceptanceId;
        public string? Id => ChildOrderId;
        public BfxOrderState State { get; internal set; } = BfxOrderState.Unknown;

        // Other fields
        public string? OrderFailedReason { get; }

        // Private properties
        List<BfxExecution> _executions = new List<BfxExecution>();

        internal BfChildOrderRequest? Request { get; }

        #region Constructors
        // Market/Limit
        public BfxChildOrder(BfChildOrderRequest request)
        {
            Request = request;

            ProductCode = request.ProductCode;
            OrderType = request.ChildOrderType;
            Side = request.Side;
            OrderSize = request.Size;
            if (OrderType == BfOrderType.Limit)
            {
                OrderPrice = request.Price;
            }
            MinuteToExpire = request.MinuteToExpire;
            TimeInForce = request.TimeInForce;

            State = BfxOrderState.Outstanding;
        }

        public BfxChildOrder(BfProductCode productCode, BfChildOrder order)
        {
            Request = default;

            // Request fields
            ProductCode = productCode;
            OrderType = order.ChildOrderType;
            Side = order.Side;
            OrderSize = order.Size;
            if (OrderType == BfOrderType.Limit)
            {
                OrderPrice = order.Price;
            }

            // Accepted
            ChildOrderAcceptanceId = order.ChildOrderAcceptanceId;

            // Confirmed
            ChildOrderId = order.ChildOrderId;
            OrderDate = order.ChildOrderDate;
            ExpireDate = order.ExpireDate;

            // Execution
            ExecutedSize = order.ExecutedSize;
            ExecutedPrice = order.AveragePrice;
            Commission = order.TotalCommission;

            switch (order.ChildOrderState)
            {
                case BfOrderState.Active:
                    State = ExecutedSize == 0 ? BfxOrderState.Ordered : BfxOrderState.Executing;
                    break;

                case BfOrderState.Completed:
                    State = BfxOrderState.Executed;
                    break;

                case BfOrderState.Expired:
                    State = BfxOrderState.Expired;
                    break;

                case BfOrderState.Canceled:
                    State = BfxOrderState.Canceled;
                    break;

                case BfOrderState.Rejected: // GetChildOrders probably does not return failed order.
                    throw new NotImplementedException();
            }
        }

        // Market/Limit get from market
        public BfxChildOrder(BfProductCode productCode, BfChildOrder order, BfPrivateExecution[] execs)
            : this(productCode, order)
        {
            _executions.AddRange(execs.Select(e => new BfxExecution(e)));
        }

        // Market/Limit/Stop/StopLimit/Trail of parent order
        public BfxChildOrder(BfParentOrderRequestParameter request, int minuteToExpire, BfTimeInForce timeInForce)
        {
            ProductCode = request.ProductCode;
            OrderType = request.ConditionType;
            Side = request.Side;
            OrderSize = request.Size;
            if (OrderType == BfOrderType.Limit || OrderType == BfOrderType.StopLimit)
            {
                OrderPrice = request.Price;
            }
            if (OrderType == BfOrderType.Stop || OrderType == BfOrderType.StopLimit)
            {
                TriggerPrice = request.TriggerPrice;
            }
            if (OrderType == BfOrderType.Trail)
            {
                TrailOffset = request.Offset;
            }
            MinuteToExpire = minuteToExpire;
            TimeInForce = timeInForce;

            State = BfxOrderState.Outstanding;

            // ChildOrderAcceptanceId will be update when update order event
        }

        // Market/Limit/Stop/StopLimit/Trail of parent order from market
        public BfxChildOrder(BfProductCode productCode, BfParentOrderDetail detail, int childIndex)
        {
            ProductCode = productCode;

            // Parameter
            var element = detail.Parameters[childIndex];
            OrderType = element.ConditionType;
            Side = element.Side;
            OrderSize = element.Size;
            if (OrderType == BfOrderType.Limit || OrderType == BfOrderType.StopLimit)
            {
                OrderPrice = element.Price;
            }
            if (OrderType == BfOrderType.Stop || OrderType == BfOrderType.StopLimit)
            {
                TriggerPrice = element.TriggerPrice;    // TriggetPrice is present in ParentOrderParameter only
            }
            if (OrderType == BfOrderType.Trail)
            {
                TrailOffset = element.Offset;           // Offset is present in ParentOrderParameter only
            }
            MinuteToExpire = detail.MinuteToExpire;

            State = BfxOrderState.Unknown;
        }
        #endregion Constructors

        internal void ApplyProductCode(BfProductCode prodctCode)
        {
            if (Request != null)
            {
                Request.ProductCode = prodctCode;
            }
            ProductCode = prodctCode;
        }

        #region Update orders
        public void Update(BfChildOrderResponse response)
        {
            State = BfxOrderState.Ordering;
            ChildOrderAcceptanceId = response.ChildOrderAcceptanceId;
        }

        public void Update(BfPrivateExecution[] execs)
        {
            if (execs.Length == 0)
            {
                return;
            }

            _executions.AddRange(execs.Select(e => new BfxExecution(e)));
            ExecutedSize = _executions.Sum(e => e.Size);
            State = OrderSize > ExecutedSize ? BfxOrderState.Executing : BfxOrderState.Executed;
        }

        public void Update(BfChildOrderEvent coe)
        {
            ChildOrderAcceptanceId = coe.ChildOrderAcceptanceId;
            ChildOrderId = coe.ChildOrderId;

            switch (coe.EventType)
            {
                case BfOrderEventType.Order:
                    OrderDate = coe.EventDate; // Is it same value to real ordered date ?
                    ExpireDate = coe.ExpireDate;
                    State = BfxOrderState.Ordered;
                    break;

                case BfOrderEventType.OrderFailed:
                    State = BfxOrderState.OrderFailed;
                    break;

                case BfOrderEventType.Cancel:
                    State = BfxOrderState.Canceled;
                    break;

                case BfOrderEventType.CancelFailed:
                    State = BfxOrderState.CancelFailed;
                    break;

                case BfOrderEventType.Execution:
                    _executions.Add(new BfxExecution(coe));
                    if (!ExecutedSize.HasValue)
                    {
                        ExecutedSize = coe.Size;
                    }
                    else
                    {
                        ExecutedSize += ExecutedSize.Value;
                    }
                    State = OrderSize > ExecutedSize ? BfxOrderState.Executing : BfxOrderState.Executed;
                    break;

                case BfOrderEventType.Expire:
                    State = BfxOrderState.Expired;
                    break;

                case BfOrderEventType.Trigger: // Parent order only
                case BfOrderEventType.Complete: // Parent order only
                    throw new NotSupportedException();
            }
        }

        public void Update(BfParentOrderEvent poe)
        {
            switch (poe.EventType)
            {
                case BfOrderEventType.Trigger:
                    ChildOrderAcceptanceId = poe.ChildOrderAcceptanceId;
                    break;
            }
        }
        #endregion Update orders
    }
}
