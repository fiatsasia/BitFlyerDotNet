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
    public class BfxParentOrder : IBfxConditionalOrder, IBfxSimpleOrder
    {
        // Common IBfxOrder fields
        public BfProductCode ProductCode { get; private set; }
        public BfOrderType OrderType { get; private set; }
        public DateTime? OrderDate { get; private set; }
        public DateTime? ExpireDate { get; private set; }
        public string? AcceptanceId => ParentOrderAcceptanceId;
        public string? Id => ParentOrderId;

        // Simple order fields
        public BfTradeSide Side { get; private set; }
        public decimal OrderSize { get; private set; }
        public decimal? OrderPrice { get; private set; }

        public decimal? TriggerPrice { get; private set; }
        public decimal? TrailOffset { get; private set; }

        public decimal? ExecutedSize { get; private set; }
        public decimal? ExecutedPrice { get; private set; }
        public decimal? Commission { get; private set; }
        public decimal? SfdCollectedAmount { get; private set; }

        // Parent order fields
        public string? ParentOrderAcceptanceId { get; private set; }
        public string? ParentOrderId { get; private set; }
        public IBfxChildOrder[] Children => _childOrders.ToArray();


        public int MinuteToExpire => throw new NotImplementedException();
        public BfTimeInForce TimeInForce => throw new NotImplementedException();
        public string? ChildOrderAcceptanceId => throw new NotImplementedException();
        public string? ChildOrderId => throw new NotImplementedException();
        public BfxOrderState State => throw new NotImplementedException();
        public string? OrderFailedReason => throw new NotImplementedException();

        // Request
        public BfParentOrderRequest? Request { get; }

        // Response

        public IBfxExecution[] Executions => _childOrders[0].Executions;

        List<BfxChildOrder> _childOrders;

        public BfxParentOrder(BfParentOrderRequest request)
        {
            Request = request;

            ProductCode = request.Parameters[0].ProductCode;
            if (request.OrderMethod == BfOrderType.Simple)
            {
                OrderType = request.Parameters[0].ConditionType;
                Side = request.Parameters[0].Side;
                OrderSize = request.Parameters[0].Size;
                if (OrderType == BfOrderType.Stop || OrderType == BfOrderType.StopLimit)
                {
                    OrderPrice = request.Parameters[0].Price;
                    TriggerPrice = request.Parameters[0].TriggerPrice;
                }
                if (OrderType == BfOrderType.Trail)
                {
                    TrailOffset = request.Parameters[0].Offset;
                }
            }
            else
            {
                OrderType = request.OrderMethod;
            }

            _childOrders = new List<BfxChildOrder>(request.Parameters.Select(e => new BfxChildOrder(e, request.MinuteToExpire, request.TimeInForce)));
        }

        public BfxParentOrder(BfProductCode productCode, BfParentOrder order, BfParentOrderDetail detail)
        {
            ProductCode = productCode;
            if (detail.OrderMethod == BfOrderType.Simple)
            {
                OrderType = detail.Parameters[0].ConditionType;
                Side = detail.Parameters[0].Side;
                OrderSize = detail.Parameters[0].Size;
                if (OrderType == BfOrderType.Stop || OrderType == BfOrderType.StopLimit)
                {
                    OrderPrice = detail.Parameters[0].Price;
                    TriggerPrice = detail.Parameters[0].TriggerPrice;
                }
                if (OrderType == BfOrderType.Trail)
                {
                    TrailOffset = detail.Parameters[0].Offset;
                }
            }
            else
            {
                OrderType = detail.OrderMethod;
            }


            ParentOrderAcceptanceId = order.ParentOrderAcceptanceId;
            ParentOrderId = detail.ParentOrderId;

            _childOrders = new List<BfxChildOrder>();
            for (int childIndex = 0; childIndex < detail.Parameters.Length; childIndex++)
            {
                _childOrders.Add(new BfxChildOrder(ProductCode, detail, childIndex));
            }
        }

        public void Update(BfParentOrderResponse response)
        {
            ParentOrderAcceptanceId = response.ParentOrderAcceptanceId;
        }

        public void Update(BfParentOrderEvent poe)
        {
            _childOrders[poe.ChildOrderIndex - 1].Update(poe);

            switch (poe.EventType)
            {
                case BfOrderEventType.Order:
                    OrderDate = poe.EventDate; // Is it same value to real ordered date ?
                    ExpireDate = poe.ExpireDate;
                    break;
            }
        }

        public void Update(BfChildOrderEvent coe)
        {
            throw new NotImplementedException();
        }
    }
}
