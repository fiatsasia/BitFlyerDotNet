﻿//==============================================================================
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
    public class BfxParentOrder : BfxOrder
    {
        public override IBfxExecution[] Executions => _orderMethod == BfOrderType.Simple ? _childOrders[0].Executions : base.Executions;
        public override IBfxOrder[] Children => _childOrders.ToArray();

        public string ParentOrderAcceptanceId { get; private set; } = string.Empty;
        public string ParentOrderId { get; private set; } = string.Empty;

        public BfParentOrderRequest? Request { get; }
        public int CompletedCount { get; private set; }

        BfOrderType _orderMethod;
        List<BfxChildOrder> _childOrders = new List<BfxChildOrder>();

        #region Create from order request and response
        public BfxParentOrder(BfParentOrderRequest request)
        {
            Request = request;
            _orderMethod = request.OrderMethod;
            _childOrders.AddRange(request.Parameters.Select(e => new BfxChildOrder(e)));

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
            ParentOrderAcceptanceId = response.ParentOrderAcceptanceId;
            ChangeState(BfxOrderState.Ordering);
        }
        #endregion

        public BfxParentOrder(BitFlyerClient client, BfProductCode productCode, BfParentOrder order)
        {
            ProductCode = productCode;

            Update(order);
            Update(client.GetParentOrder(ProductCode, parentOrderId: order.ParentOrderId).GetContent());
            Update(client.GetChildOrders(ProductCode, parentOrderId: order.ParentOrderId).GetContent().OrderBy(e => e.PagingId).ToArray());
            foreach (var childOrder in _childOrders)
            {
                if (!string.IsNullOrEmpty(childOrder.ChildOrderId))
                {
                    childOrder.Update(client.GetPrivateExecutions(productCode, childOrderId: childOrder.ChildOrderId).GetContent());
                }
            }
        }

        public void Update(BfParentOrder order)
        {
            ParentOrderId = order.ParentOrderId;
            ParentOrderAcceptanceId = order.ParentOrderAcceptanceId;
            _orderMethod = order.ParentOrderType;

            ExpireDate = order.ExpireDate;
            OrderDate = order.ParentOrderDate;

            ExecutedSize = order.ExecutedSize;
            Commission = order.TotalCommission;

            // API 仕様的に実質 Active 状態しか取得できない。
            switch (order.ParentOrderState)
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
        }

        void Update(BfParentOrderDetail order)
        {
            ParentOrderId = order.ParentOrderId;
            MinuteToExpire = order.MinuteToExpire;

            if (_childOrders.Count == 0)
            {
                for (int childIndex = 0; childIndex < order.Parameters.Length; childIndex++)
                {
                    var childOrder = new BfxChildOrder(ProductCode, order.Parameters[childIndex]);
                    childOrder.MinuteToExpire = order.MinuteToExpire;
                    _childOrders.Add(childOrder);
                }
            }
            else
            {
                for (int childIndex = 0; childIndex < _childOrders.Count; childIndex++)
                {
                    _childOrders[childIndex].Update(order.Parameters[childIndex]);
                    _childOrders[childIndex].MinuteToExpire = order.MinuteToExpire;
                }
            }

            if (order.OrderMethod == BfOrderType.Simple)
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
                OrderType = order.OrderMethod;
            }
        }

        public void Update(BfChildOrder[] childOrders)
        {
            if (childOrders.Length == 0) // input is empty
            {
                return;
            }
            else if (_childOrders.Count == childOrders.Length) // input it full of content
            {
                for (int childOrderIndex = 0; childOrderIndex < _childOrders.Count; childOrderIndex++)
                {
                    _childOrders[childOrderIndex].Update(childOrders[childOrderIndex]);
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
                        if (newOrder.ChildOrderAcceptanceId == currentOrder.ChildOrderAcceptanceId)
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
                        if (childOrder.ChildOrderState == BfOrderState.Completed)
                        {
                            CompletedCount = 1;
                        }
                        return;

                    case BfOrderType.OCO:
                        // Probably completed
                        break;

                    case BfOrderType.IFDOCO:
                        _childOrders[0].Update(childOrder);
                        if (childOrder.ChildOrderState == BfOrderState.Completed)
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
                    Debug.WriteLine($"BfOrderEventType.Complete received. count = {CompletedCount}");
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
            var childOrderIndex = _childOrders.FindIndex(e => e.ChildOrderAcceptanceId == coe.ChildOrderAcceptanceId);
            _childOrders[childOrderIndex].Update(coe);
            return childOrderIndex;
        }

        void ChangeState(BfxOrderState state)
        {
            Debug.WriteLine($"Parent order status changed: {ParentOrderAcceptanceId} {State} -> {state}");
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

            ChangeState(BfxOrderState.Outstanding);

            Request.Parameters.ForEach(e => e.ProductCode = productCode);
            _childOrders.ForEach(e => e.ApplyParameters(productCode, minutesToExpire, timeInForce));
        }
    }
}
