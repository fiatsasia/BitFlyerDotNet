//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public class DbParentOrder
    {
        [Column(Order = 0)]
        public uint PagingId { get; set; }

        [Required]
        [Column(Order = 1)]
        public string ProductCode { get; set; }

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
        public BfTimeInForce? TimeInForce { get; set; }

        [Column(Order = 9)]
        public string OrderFailedReason { get; set; }

        [NotMapped]
        public DbChildOrder[] Children { get; set; }

        public DbParentOrder()
        {
        }

        public DbParentOrder(string productCode, BfParentOrder req, string parentOrderAcceptanceId)
        {
            ProductCode = productCode;
            OrderType = req.OrderMethod;
            TimeInForce = req.TimeInForce;
            if (TimeInForce == BfTimeInForce.NotSpecified)
            {
                TimeInForce = BfTimeInForce.GTC;
            }
            AcceptanceId = parentOrderAcceptanceId;
        }

        public DbParentOrder(string productCode, BfParentOrderDetailStatus order)
        {
            ProductCode = productCode;

            PagingId = order.PagingId;
            OrderId = order.ParentOrderId;
            OrderType = order.OrderMethod;
            ExpireDate = order.ExpireDate;
            TimeInForce = order.TimeInForce;
            AcceptanceId = order.ParentOrderAcceptanceId;
        }

        public DbParentOrder(string productCode, BfParentOrderStatus order, BfParentOrderDetailStatus detail)
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

        public void Update(BfParentOrderStatus order, BfParentOrderDetailStatus detail)
        {
            PagingId = order.PagingId;
            State = order.ParentOrderState;
            OrderDate = order.ParentOrderDate;
            TimeInForce = detail.TimeInForce;
        }

        public DbParentOrder(string productCode, BfParentOrderEvent e)
        {
            ProductCode = productCode;
            if (e.ParentOrderType.HasValue) OrderType = e.ParentOrderType.Value;
            Update(e);
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
                ExpireDate = poe.ExpireDate.Value;
            }
            if (poe.EventType == BfOrderEventType.OrderFailed)
            {
                OrderFailedReason = poe.OrderFailedReason;
            }
        }
    }
}
