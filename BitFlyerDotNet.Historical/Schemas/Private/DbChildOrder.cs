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
    public class DbChildOrder : IBfChildOrder
    {
        [Key]
        [Column(Order = 0)]
        public uint Id { get; set; }

        [Column(Order = 1)]
        public uint PagingId { get; set; }

        [Required]
        [Column(Order = 2)]
        public BfProductCode ProductCode { get; set; }

        [Required]
        [Column(Order = 3)]
        public BfTradeSide Side { get; set; }

        [Required]
        [Column(Order = 4)]
        public BfOrderType OrderType { get; set; }

        [Column(Order = 5)]
        public decimal? OrderPrice { get; set; }

        [Required]
        [Column(Order = 6)]
        public decimal OrderSize { get; set; }

        [Column(Order = 7)]
        public decimal? TriggerPrice { get; set; }

        [Column(Order = 8)]
        public decimal? Offset { get; set; }

        [Column(Order = 9)]
        public int? MinuteToExpire { get; set; }

        [Column(Order = 10)]
        public BfTimeInForce? TimeInForce { get; set; }

        [Column(Order = 11)]
        public string AcceptanceId { get; set; }

        [Column(Order = 12)]
        public string OrderId { get; set; }

        [Column(Order = 13)]
        public BfOrderState State { get; set; }

        [Column(Order = 14)]
        public DateTime? OrderDate { get; set; }

        [Column(Order = 15)]
        public DateTime? ExpireDate { get; set; }

        [Column(Order = 16)]
        public decimal? ExecutedSize { get; set; }

        [Column(Order = 17)]
        public DateTime? CloseDate { get; set; }

        [Column(Order = 18)]
        public string FailedReason { get; set; }

        // Parent linkage
        [Column(Order = 19)]
        public string ParentOrderAcceptanceId { get; set; }

        [Column(Order = 20)]
        public string ParentOrderId { get; set; }

        [Column(Order = 21)]
        public int ChildOrderIndex { get; set; }

        [NotMapped]
        public IBfPrivateExecution[] Executions { get; set; }

        public DbChildOrder()
        {
        }

        public DbChildOrder(BfChildOrderRequest request, string childOrderAcceptanceId)
        {
            AcceptanceId = childOrderAcceptanceId;

            ProductCode = request.ProductCode;
            OrderType = request.ChildOrderType;
            Side = request.Side;
            if (OrderType == BfOrderType.Limit)
            {
                OrderPrice = request.Price;
            }
            OrderSize = request.Size;
            MinuteToExpire = request.MinuteToExpire;

            ChildOrderIndex = -1;
        }

        public DbChildOrder(BfProductCode productCode, BfChildOrder order)
        {
            ProductCode = productCode;
            Side = order.Side;
            OrderType = order.ChildOrderType;
            OrderSize = order.Size;

            ChildOrderIndex = -1;

            Update(order);
        }

        public void Update(BfChildOrder order)
        {
            PagingId = order.PagingId;
            AcceptanceId = order.ChildOrderAcceptanceId;
            OrderId = order.ChildOrderId;
            if (order.ChildOrderType != BfOrderType.Market)
            {
                OrderPrice = order.Price;
            }
            State = order.ChildOrderState;
            ExpireDate = order.ExpireDate;
            OrderDate = order.ChildOrderDate;
            ExecutedSize = order.ExecutedSize;
        }

        public void Update(BfChildOrderEvent coe)
        {
            OrderId = coe.ChildOrderId;
            AcceptanceId = coe.ChildOrderAcceptanceId; // When child of parent

            switch (coe.EventType)
            {
                case BfOrderEventType.Order:
                    State = BfOrderState.Active;
                    OrderPrice = coe.Price;
                    OrderDate = coe.EventDate;
                    ExpireDate = coe.ExpireDate;
                    break;

                case BfOrderEventType.OrderFailed:
                    State = BfOrderState.Rejected;
                    FailedReason = coe.OrderFailedReason;
                    CloseDate = coe.EventDate;
                    break;

                case BfOrderEventType.Cancel:
                    State = BfOrderState.Canceled;
                    CloseDate = coe.EventDate;
                    break;

                case BfOrderEventType.CancelFailed:
                    State = BfOrderState.Rejected;
                    CloseDate = coe.EventDate;
                    FailedReason = "Cancel Failed";
                    break;

                case BfOrderEventType.Execution:
                    if (!ExecutedSize.HasValue)
                    {
                        ExecutedSize = coe.Size;
                    }
                    else
                    {
                        ExecutedSize += coe.Size;
                    }
                    if (ExecutedSize >= OrderSize)
                    {
                        State = BfOrderState.Completed;
                        CloseDate = coe.EventDate;
                    }
                    else
                    {
                        State = BfOrderState.Active;
                    }
                    break;

                case BfOrderEventType.Expire:
                    State = BfOrderState.Expired;
                    CloseDate = coe.EventDate;
                    break;

                default:
                    throw new ArgumentException($"{coe.EventType} is not expected.");
            }
        }

        //======================================================================
        // From element of parent order
        //

        public DbChildOrder(BfParentOrderRequest req, BfParentOrderResponse resp, int childOrderIndex)
        {
            ProductCode = req.Parameters[childOrderIndex].ProductCode;
            Side = req.Parameters[childOrderIndex].Side;
            OrderType = req.Parameters[childOrderIndex].ConditionType;
            OrderSize = req.Parameters[childOrderIndex].Size;
            MinuteToExpire = req.MinuteToExpire;
            TimeInForce = req.TimeInForce;

            if (OrderType is BfOrderType.Limit or BfOrderType.StopLimit)
            {
                OrderPrice = req.Parameters[childOrderIndex].Price;
            }
            if (OrderType is BfOrderType.Stop or BfOrderType.StopLimit)
            {
                TriggerPrice = req.Parameters[childOrderIndex].TriggerPrice;
            }
            if (OrderType == BfOrderType.Trail)
            {
                Offset = req.Parameters[childOrderIndex].Offset;
            }

            ParentOrderAcceptanceId = resp.ParentOrderAcceptanceId;
            ChildOrderIndex = childOrderIndex;
        }

        public DbChildOrder(BfProductCode productCode, BfParentOrderDetail order, int childOrderIndex)
        {
            ProductCode = productCode;
            OrderType = order.Parameters[childOrderIndex].ConditionType;
            Side = order.Parameters[childOrderIndex].Side;
            OrderSize = order.Parameters[childOrderIndex].Size;
            ExpireDate = order.ExpireDate;
            TimeInForce = order.TimeInForce;

            if (OrderType is BfOrderType.Limit or BfOrderType.StopLimit)
            {
                OrderPrice = order.Parameters[childOrderIndex].Price;
            }
            if (OrderType is BfOrderType.Stop or BfOrderType.StopLimit)
            {
                TriggerPrice = order.Parameters[childOrderIndex].TriggerPrice;
            }
            if (OrderType == BfOrderType.Trail)
            {
                Offset = order.Parameters[childOrderIndex].Offset;
            }

            ParentOrderAcceptanceId = order.ParentOrderAcceptanceId;
            ParentOrderId = order.ParentOrderId;
            ChildOrderIndex = childOrderIndex;
        }

        public void Update(BfParentOrderEvent poe)
        {
            ParentOrderAcceptanceId = poe.ParentOrderAcceptanceId;
            ParentOrderId = poe.ParentOrderId;
            AcceptanceId = poe.ChildOrderAcceptanceId;
            ChildOrderIndex = poe.ChildOrderIndex;

            switch (poe.EventType)
            {
                case BfOrderEventType.Trigger:
                    ExpireDate = poe.ExpireDate;
                    break;

                case BfOrderEventType.Complete:
                    break;
            }
        }
    }
}
