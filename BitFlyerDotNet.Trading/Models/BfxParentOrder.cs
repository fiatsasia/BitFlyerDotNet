//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxParentOrder : BfxOrder
    {
        public override IBfxExecution[] Executions => _orderMethod == BfOrderType.Simple ? _childOrders[0].Executions : base.Executions;
        public override IBfxOrder[] Children => _childOrders.ToArray();

        public override string AcceptanceId { get; protected set; } = string.Empty;
        public override string OrderId { get; protected set; } = string.Empty;

        public BfParentOrderRequest? Request { get; }
        public int CompletedCount { get; private set; }

        BfOrderType _orderMethod;
        BfxChildOrder[] _childOrders;

        #region Create from order request and response
        public BfxParentOrder(BfOrderType orderType)
        {
            _childOrders = new BfxChildOrder[orderType.GetChildCount()];
        }

        public BfxParentOrder(BfParentOrderRequest request)
            : this(request.OrderMethod)
        {
            Request = request;
            _orderMethod = request.OrderMethod;
            _childOrders = request.Parameters.Select(e => new BfxChildOrder(e)).ToArray();

            ProductCode = _childOrders[0].ProductCode;
            if (request.OrderMethod == BfOrderType.Simple)
            {
                var childOrder = _childOrders[0];
                OrderType = childOrder.OrderType;
                Side = childOrder.Side;
                OrderSize = childOrder.OrderSize;
                OrderPrice = childOrder.OrderPrice;
                TriggerPrice = childOrder.TriggerPrice;
                TrailOffset = childOrder.TrailOffset;
            }
            else
            {
                OrderType = request.OrderMethod;
            }
        }

        public void Update(BfParentOrderResponse response)
        {
            AcceptanceId = response.ParentOrderAcceptanceId;
        }
        #endregion

        public BfxParentOrder(IBfParentOrder order)
            : this(order.OrderType)
        {
            ProductCode = order.ProductCode;
            _childOrders = new BfxChildOrder[order.OrderType.GetChildCount()];
            Update(order);
        }

        public void Update(IBfParentOrder order)
        {
            OrderId = order.OrderId;
            AcceptanceId = order.AcceptanceId;
            _orderMethod = order.OrderType;

            ExpireDate = order.ExpireDate;
            OrderDate = order.OrderDate;

            // API 仕様的に実質 Active 状態しか取得できない。
            switch (order.State)
            {
                case BfOrderState.Active:
                    ChangeState(BfxOrderState.Ordered);
                    break;

                case BfOrderState.Completed:
                    ChangeState(BfxOrderState.Completed);
                    break;

                case BfOrderState.Canceled:
                    ChangeState(BfxOrderState.Canceled);
                    break;

                case BfOrderState.Expired:
                    ChangeState(BfxOrderState.Expired);
                    break;

                default:
                    throw new ArgumentException("Unexpected parent order state.");
            }

            Update(order.Children);
        }

        public void Update(IBfChildOrder[] childOrders)
        {
            if (childOrders.Length == 0) // input is empty
            {
                return;
            }
            else if (_childOrders.Length == childOrders.Length) // input it full of content
            {
                for (int childOrderIndex = 0; childOrderIndex < _childOrders.Length; childOrderIndex++)
                {
                    if (_childOrders[childOrderIndex] == default)
                    {
                        _childOrders[childOrderIndex] = new BfxChildOrder(childOrders[childOrderIndex]);
                    }
                    else
                    {
                        _childOrders[childOrderIndex].Update(childOrders[childOrderIndex]);
                    }
                }
                return;
            }
            else // matching with ID
            {
                var updatedCount = 0;
                foreach (var newOrder in childOrders)
                {
                    foreach (var currentOrder in _childOrders)
                    {
                        if (newOrder.AcceptanceId == currentOrder.AcceptanceId)
                        {
                            currentOrder.Update(newOrder);
                            updatedCount++;
                        }
                    }
                }
                if (updatedCount == childOrders.Length)
                {
                    return; // all inputs are matched
                }
            }

            if (childOrders.Length == 1)
            {
                var childOrder = childOrders[0];
                switch (OrderType)
                {
                    case BfOrderType.IFD:
                        _childOrders[0].Update(childOrder);
                        if (childOrder.State == BfOrderState.Completed)
                        {
                            CompletedCount = 1;
                        }
                        return;

                    case BfOrderType.OCO:
                        // Probably completed
                        break;

                    case BfOrderType.IFDOCO:
                        _childOrders[0].Update(childOrder);
                        if (childOrder.State == BfOrderState.Completed)
                        {
                            CompletedCount = 1;
                        }
                        return;

                    default:
                        throw new ArgumentException();
                }
            }

            if (childOrders.Length == 2)
            {
                switch (OrderType)
                {
                    case BfOrderType.IFDOCO:
                        _childOrders[0].Update(childOrders[0]);
                        break;

                    default:
                        throw new ArgumentException();
                }
            }
        }

        public void Update(BfParentOrderEvent poe)
        {
            _orderMethod = poe.ParentOrderType;
            LastUpdatedTime = poe.EventDate;
            switch (poe.EventType)
            {
                case BfOrderEventType.Order:
                    OrderDate = poe.EventDate; // Is it same value to real ordered date ?
                    ExpireDate = poe.ExpireDate;
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

                case BfOrderEventType.Trigger:
                    _childOrders[poe.ChildOrderIndex - 1].Update(poe);
                    break;

                case BfOrderEventType.Expire:
                    ChangeState(BfxOrderState.Expired);
                    break;

                case BfOrderEventType.Complete:
                    CompletedCount++;
                    Log.Trace($"BfOrderEventType.Complete received. count = {CompletedCount}");
                    switch (OrderType)
                    {
                        case BfOrderType.IFD:
                        case BfOrderType.IFDOCO:
                            if (CompletedCount == 2)
                            {
                                ChangeState(BfxOrderState.Completed);
                            }
                            break;
                    }
                    break;

                case BfOrderEventType.Unknown:
                case BfOrderEventType.Execution:
                    throw new NotSupportedException();
            }
        }

        public int Update(BfChildOrderEvent coe)
        {
            var childOrderIndex = Array.FindIndex(_childOrders, e => e.AcceptanceId == coe.ChildOrderAcceptanceId);
            _childOrders[childOrderIndex].Update(coe);
            return childOrderIndex;
        }

        void ChangeState(BfxOrderState state)
        {
            Log.Trace($"Parent order status changed: {AcceptanceId} {State} -> {state}");
            State = state;
        }

        internal override void ApplyParameters(BfProductCode productCode, int minutesToExpire, BfTimeInForce timeInForce)
        {
            if (Request == null)
            {
                throw new ArgumentException();
            }

            ProductCode = productCode;
            MinuteToExpire = minutesToExpire;
            TimeInForce = timeInForce;

            if (_orderMethod == BfOrderType.Simple)
            {
                var childOrder = _childOrders[0];
                childOrder.MinuteToExpire = MinuteToExpire;
                childOrder.TimeInForce = TimeInForce;
            }

            Request.Parameters.ForEach(e => e.ProductCode = productCode);
            _childOrders.ForEach(e => e.ApplyParameters(productCode, minutesToExpire, timeInForce));
        }
    }
}
