//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Generic;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxChildOrder : BfxOrder
    {
        public override IBfxExecution[] Executions => _executions.ToArray();

        // Response fields
        public override string AcceptanceId { get; protected set; } = string.Empty;
        public override string OrderId { get; protected set; } = string.Empty;

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

        // Market/Limit get from market
        public BfxChildOrder(IBfChildOrder order)
        {
            Request = default;

            // Request fields
            ProductCode = order.ProductCode;
            OrderType = order.OrderType;
            Side = order.Side;
            OrderSize = order.OrderSize;
            if (OrderType == BfOrderType.Limit)
            {
                OrderPrice = order.OrderPrice;
            }

            Update(order);
            LastUpdatedTime = order.OrderDate;
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
        public BfxChildOrder(BfProductCode productCode, BfParentOrderParameter order)
        {
            ProductCode = productCode;
            Update(order);
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
        }

        #region Update orders
        public void Update(BfChildOrderResponse response)
        {
            AcceptanceId = response.ChildOrderAcceptanceId;
        }

        public void Update(IBfPrivateExecution[] execs)
        {
            if (execs.Length == 0)
            {
                return;
            }

            _executions.Clear();
            _executions.AddRange(execs.Select(e => new BfxExecution(e)));
            ExecutedSize = _executions.Sum(e => e.Size);
            ChangeState(OrderSize > ExecutedSize ? BfxOrderState.PartiallyExecuted : BfxOrderState.Executed);
            LastUpdatedTime = _executions.Last().Time;
        }

        public void Update(BfParentOrderParameter order)
        {
            OrderType = order.ConditionType;
            Side = order.Side;
            OrderSize = order.Size;
            if (OrderType == BfOrderType.Limit || OrderType == BfOrderType.StopLimit)
            {
                OrderPrice = order.Price;
            }
            if (OrderType == BfOrderType.Stop || OrderType == BfOrderType.StopLimit)
            {
                TriggerPrice = order.TriggerPrice;    // TriggetPrice is present in ParentOrderParameter only
            }
            if (OrderType == BfOrderType.Trail)
            {
                TrailOffset = order.Offset;           // Offset is present in ParentOrderParameter only
            }
        }

        public void Update(IBfChildOrder order)
        {
            AcceptanceId = order.AcceptanceId;
            OrderId = order.OrderId;
            OrderDate = order.OrderDate;
            ExpireDate = order.ExpireDate;

            switch (order.State)
            {
                case BfOrderState.Active:
                    ChangeState(ExecutedSize == 0m ? BfxOrderState.Ordered : BfxOrderState.PartiallyExecuted);
                    break;

                case BfOrderState.Completed:
                    ChangeState(BfxOrderState.Executed);
                    break;

                case BfOrderState.Canceled:
                    ChangeState(BfxOrderState.Canceled);
                    break;

                case BfOrderState.Expired:
                    ChangeState(BfxOrderState.Expired);
                    break;

                default:
                    throw new ArgumentException("Unexpected order status");
            }
            Update(order.Executions);
        }

        public void Update(BfChildOrderEvent coe)
        {
            if (!string.IsNullOrEmpty(coe.ChildOrderAcceptanceId))
            {
                AcceptanceId = coe.ChildOrderAcceptanceId;
            }
            if (!string.IsNullOrEmpty(coe.ChildOrderId))
            {
                OrderId = coe.ChildOrderId;
            }
            LastUpdatedTime = coe.EventDate;

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
                    if (State != BfxOrderState.Outstanding) // Sometimes recived before ordered which under canceled OCO
                    {
                        ChangeState(BfxOrderState.CancelFailed);
                    }
                    break;

                case BfOrderEventType.Execution:
                    _executions.Add(new BfxExecution(coe));
                    ExecutedSize = _executions.Sum(e => e.Size);
                    ExecutedPrice = Math.Round(_executions.Sum(e => e.Price * e.Size) / ExecutedSize.Value, ProductCode.GetPriceDecimals(), MidpointRounding.ToEven);
                    ChangeState(OrderSize > ExecutedSize ? BfxOrderState.PartiallyExecuted : BfxOrderState.Executed);
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

            AcceptanceId = poe.ChildOrderAcceptanceId;
        }

        void ChangeState(BfxOrderState state)
        {
            Log.Trace($"Child order status changed: {AcceptanceId} {State} -> {state}");
            State = state;
        }
        #endregion Update orders
    }
}
