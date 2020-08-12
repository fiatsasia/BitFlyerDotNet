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
    public class BfxChildOrder : BfxOrder
    {
        public override string? AcceptanceId => ChildOrderAcceptanceId;
        public override string? OrderId => ChildOrderId;
        public override IBfxExecution[] Executions => _executions.ToArray();

        // Response fields
        public string? ChildOrderAcceptanceId { get; private set; }
        public string? ChildOrderId { get; private set; }

        internal BfChildOrderRequest? Request { get; }

        // Private properties
        List<BfxExecution> _executions = new List<BfxExecution>();

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
                    ChangeState(ExecutedSize == 0 ? BfxOrderState.Ordered : BfxOrderState.Executing);
                    break;

                case BfOrderState.Completed:
                    ChangeState(BfxOrderState.Executed);
                    break;

                case BfOrderState.Expired:
                    ChangeState(BfxOrderState.Expired);
                    break;

                case BfOrderState.Canceled:
                    ChangeState(BfxOrderState.Canceled);
                    break;

                case BfOrderState.Rejected: // GetChildOrders probably does not return failed order.
                    throw new NotSupportedException();
            }

            LastUpdatedTime = order.ChildOrderDate;
        }

        // Market/Limit get from market
        public BfxChildOrder(BfProductCode productCode, BfChildOrder order, BfPrivateExecution[] execs)
            : this(productCode, order)
        {
            _executions.AddRange(execs.Select(e => new BfxExecution(e)));
            if (_executions.Count > 0)
            {
                LastUpdatedTime = _executions.Last().Time;
            }
        }

        // Market/Limit/Stop/StopLimit/Trail of parent order
        public BfxChildOrder(BfParentOrderRequestParameter request)
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

            ChangeState(BfxOrderState.Unknown);
        }
        #endregion Constructors

        internal override void ApplyParameters(BfProductCode productCode, int minutesToExpire, BfTimeInForce timeInForce)
        {
            if (Request != null)
            {
                Request.ProductCode = productCode;
                Request.MinuteToExpire = minutesToExpire;
                Request.TimeInForce = TimeInForce;
            }
            ProductCode = productCode;
            MinuteToExpire = minutesToExpire;
            TimeInForce = TimeInForce;

            ChangeState(BfxOrderState.Outstanding);
        }

        #region Update orders
        public void Update(BfChildOrderResponse response)
        {
            ChildOrderAcceptanceId = response.ChildOrderAcceptanceId;
            ChangeState(BfxOrderState.Ordering);
        }

        public void Update(BfPrivateExecution[] execs)
        {
            if (execs.Length == 0)
            {
                return;
            }

            _executions.AddRange(execs.Select(e => new BfxExecution(e)));
            ExecutedSize = _executions.Sum(e => e.Size);
            ChangeState(OrderSize > ExecutedSize ? BfxOrderState.Executing : BfxOrderState.Executed);
            LastUpdatedTime = _executions.Last().Time;
        }

        public void Update(BfChildOrderEvent coe)
        {
            if (coe.ChildOrderType == BfOrderType.Unknown)
            {
                Debug.WriteLine($"ChildOrderAcceptanceId: {coe.ChildOrderAcceptanceId} ChildOrderId: {ChildOrderId} EventDate: {coe.EventDate}");
                ChildOrderAcceptanceId = coe.ChildOrderAcceptanceId;
                LastUpdatedTime = coe.EventDate;
            }
            else
            {
                ChildOrderAcceptanceId = coe.ChildOrderAcceptanceId;
                ChildOrderId = coe.ChildOrderId;
                LastUpdatedTime = coe.EventDate;
            }

            switch (coe.EventType)
            {
                case BfOrderEventType.Order:
                    OrderDate = coe.EventDate; // Is it same value to real ordered date ?
                    ExpireDate = coe.ExpireDate;
                    ChangeState(BfxOrderState.Ordered);
                    break;

                case BfOrderEventType.OrderFailed:
                    ChangeState(BfxOrderState.OrderFailed);
                    break;

                case BfOrderEventType.Cancel:
                    ChangeState(BfxOrderState.Canceled);
                    break;

                case BfOrderEventType.CancelFailed:
                    ChangeState(BfxOrderState.CancelFailed);
                    break;

                case BfOrderEventType.Execution:
                    _executions.Add(new BfxExecution(coe));
                    ExecutedSize = _executions.Sum(e => e.Size);
                    ExecutedPrice = Math.Round(_executions.Sum(e => e.Price * e.Size) / ExecutedSize.Value, ProductCode.GetPriceDecimals(), MidpointRounding.ToEven);
                    ChangeState(OrderSize > ExecutedSize ? BfxOrderState.Executing : BfxOrderState.Executed);
                    break;

                case BfOrderEventType.Expire:
                    ChangeState(BfxOrderState.Expired);
                    break;

                case BfOrderEventType.Unknown:
                case BfOrderEventType.Trigger: // Parent order only
                case BfOrderEventType.Complete: // Parent order only
                    throw new NotSupportedException();
            }
        }

        // Update by parent order event
        public void Update(BfParentOrderEvent poe)
        {
            if (poe.EventType != BfOrderEventType.Trigger)
            {
                return;
            }

            ChildOrderAcceptanceId = poe.ChildOrderAcceptanceId;
        }

        void ChangeState(BfxOrderState state)
        {
            Debug.WriteLine($"Child order status changed: {ChildOrderAcceptanceId} {State} -> {state}");
            State = state;
        }
        #endregion Update orders
    }
}
