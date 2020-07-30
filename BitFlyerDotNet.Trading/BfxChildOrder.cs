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
    public class BfxChildOrder : IBfxSimpleOrder, IBfxChildOrder
    {
        // Request fields
        public BfProductCode ProductCode { get; }
        public BfOrderType OrderType { get; }
        public BfTradeSide Side { get; }
        public decimal OrderSize { get; }
        public decimal? OrderPrice { get; } // Missing if market price order
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
        public BfxOrderState State { get; internal set; }

        // Other fields
        public string? OrderFailedReason { get; }

        // Status management
        public BfxOrderState TransitState(BfxOrderState newState) => State = State.TransitState(newState);
        public BfxOrderState TransitState(BfOrderEventType evt) => State = State.TransitState(evt);

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
                    State = ExecutedSize == 0 ? BfxOrderState.OrderConfirmed : BfxOrderState.Executing;
                    break;

                case BfOrderState.Completed:
                    State = BfxOrderState.Executed;
                    break;

                case BfOrderState.Expired:
                    State = BfxOrderState.Expired;
                    break;

                case BfOrderState.Canceled: // GetChildOrders does not return canceled order.
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
        }
        #endregion Constructors

        #region Update orders
        public void Update(BfChildOrderResponse response)
        {
            ChildOrderAcceptanceId = response.ChildOrderAcceptanceId;
        }

        public void Update(BfPrivateExecution[] execs)
        {
            _executions.AddRange(execs.Select(e => new BfxExecution(e)));
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
                    break;

                case BfOrderEventType.Execution:
                    Debug.Assert(OrderType == coe.ChildOrderType);
                    _executions.Add(new BfxExecution(coe));
                    if (!ExecutedSize.HasValue)
                    {
                        ExecutedSize = coe.Size;
                    }
                    else
                    {
                        ExecutedSize += ExecutedSize.Value;
                    }
                    break;

                case BfOrderEventType.Cancel:
                    break;

                case BfOrderEventType.Complete:
                    break;

                // Probably parent order only
                case BfOrderEventType.Trigger:
                    break;
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
