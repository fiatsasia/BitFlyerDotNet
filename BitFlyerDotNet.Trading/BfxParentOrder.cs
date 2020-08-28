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
    public class BfxParentOrder : BfxOrder
    {
        public override string? AcceptanceId => ParentOrderAcceptanceId;
        public override string? OrderId => ParentOrderId;
        public override IBfxExecution[] Executions => _orderMethod == BfOrderType.Simple ? _childOrders[0].Executions : base.Executions;
        public override IBfxOrder[] Children => _childOrders.ToArray();

        public string? ParentOrderAcceptanceId { get; private set; }
        public string? ParentOrderId { get; private set; }

        public BfParentOrderRequest? Request { get; }
        public int CompletedCount { get; private set; }

        BfOrderType _orderMethod;
        List<BfxChildOrder> _childOrders;

        public BfxParentOrder(BfParentOrderRequest request)
        {
            Request = request;
            _orderMethod = request.OrderMethod;
            _childOrders = new List<BfxChildOrder>(request.Parameters.Select(e => new BfxChildOrder(e)));

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

        public BfxParentOrder(BfProductCode productCode, BfParentOrder order, BfParentOrderDetail detail)
        {
            _orderMethod = order.ParentOrderType;
            _childOrders = new List<BfxChildOrder>();
            for (int childIndex = 0; childIndex < detail.Parameters.Length; childIndex++)
            {
                _childOrders.Add(new BfxChildOrder(ProductCode, detail, childIndex));
            }

            ProductCode = productCode;
            if (detail.OrderMethod == BfOrderType.Simple)
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
                OrderType = detail.OrderMethod;
            }

            ParentOrderAcceptanceId = order.ParentOrderAcceptanceId;
            ParentOrderId = detail.ParentOrderId;
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

        public void Update(BfParentOrderResponse response)
        {
            ParentOrderAcceptanceId = response.ParentOrderAcceptanceId;
            ChangeState(BfxOrderState.Ordering);
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
    }
}
