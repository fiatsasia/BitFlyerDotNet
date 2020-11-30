//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public class DbParentOrder : IBfParentOrder
    {
        [Column(Order = 0)]
        public uint PagingId { get; set; }

        [Required]
        [Column(Order = 1)]
        public BfProductCode ProductCode { get; set; }

        [Required]
        [Column(Order = 2)]
        public BfOrderType OrderType { get; set; }

        [Required]
        [Column(Order = 3)]
        public string AcceptanceId { get; set; }

        [Column(Order = 4)]
        public string OrderId { get; set; }

        [Column(Order = 5)]
        public BfOrderState State { get; set; }

        [Column(Order = 6)]
        public DateTime OrderDate { get; set; }

        [Column(Order = 7)]
        public DateTime ExpireDate { get; set; }

        [Column(Order = 8)]
        public int MinuteToExpire { get; set; }

        [Column(Order = 9)]
        public BfTimeInForce TimeInForce { get; set; }

        [Column(Order = 10)]
        public string OrderFailedReason { get; set; }

        [NotMapped]
        public IBfChildOrder[] Children { get; set; }

        public DbParentOrder()
        {
        }

        public DbParentOrder(BfProductCode productCode, BfParentOrderRequest req, BfParentOrderResponse resp)
        {
            ProductCode = productCode;
            OrderType = req.OrderMethod;
            MinuteToExpire = req.MinuteToExpire;
            TimeInForce = req.TimeInForce;
            if (TimeInForce == BfTimeInForce.NotSpecified)
            {
                TimeInForce = BfTimeInForce.GTC;
            }
            AcceptanceId = resp.ParentOrderAcceptanceId;
            MinuteToExpire = req.MinuteToExpire;
        }

        public DbParentOrder(BfProductCode productCode, BfParentOrderDetail order)
        {
            ProductCode = productCode;

            PagingId = order.PagingId;
            OrderId = order.ParentOrderId;
            OrderType = order.OrderMethod;
            ExpireDate = order.ExpireDate;
            TimeInForce = order.TimeInForce;
            AcceptanceId = order.ParentOrderAcceptanceId;
        }

        public DbParentOrder(BfProductCode productCode, BfParentOrder order, BfParentOrderDetail detail)
        {
            ProductCode = productCode;

            PagingId = order.PagingId;
            OrderId = order.ParentOrderId;
            OrderType = order.ParentOrderType;
            ExpireDate = order.ExpireDate;
            TimeInForce = detail.TimeInForce;
            AcceptanceId = order.ParentOrderAcceptanceId;
            State = order.ParentOrderState;
            OrderDate = order.ParentOrderDate;
        }

        public void Update(BfParentOrder order, BfParentOrderDetail detail)
        {
            PagingId = order.PagingId;
            State = order.ParentOrderState;
            OrderDate = order.ParentOrderDate;
            TimeInForce = detail.TimeInForce;
        }

        public DbParentOrder(BfProductCode productCode, BfParentOrderEvent poe)
        {
            ProductCode = productCode;
            OrderType = poe.ParentOrderType;
            Update(poe);
        }

        public void Update(BfParentOrderEvent poe)
        {
            AcceptanceId = poe.ParentOrderAcceptanceId;
            OrderId = poe.ParentOrderId;
            State = poe.EventType switch
            {
                BfOrderEventType.Complete => BfOrderState.Completed,
                BfOrderEventType.Cancel => BfOrderState.Canceled,
                BfOrderEventType.Expire => BfOrderState.Expired,
                BfOrderEventType.OrderFailed => BfOrderState.Rejected,
                _ => BfOrderState.Active
            };
            if (poe.EventType == BfOrderEventType.Order)
            {
                OrderDate = poe.EventDate;
                ExpireDate = poe.ExpireDate;
            }
            if (poe.EventType == BfOrderEventType.OrderFailed)
            {
                OrderFailedReason = poe.OrderFailedReason;
            }
        }
    }
}
