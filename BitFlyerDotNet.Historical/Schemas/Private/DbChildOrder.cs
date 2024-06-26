﻿//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Historical;

public class DbChildOrder
{
    [Key]
    [Column(Order = 0)]
    public uint Id { get; set; }

    [Column(Order = 1)]
    public long PagingId { get; set; }

    [Required]
    [Column(Order = 2)]
    public string ProductCode { get; set; }

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
    public DbPrivateExecution[] Executions { get; set; }

    public DbChildOrder()
    {
    }

    public DbChildOrder(BfChildOrder request, string childOrderAcceptanceId)
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
        TimeInForce = request.TimeInForce;
        MinuteToExpire = request.MinuteToExpire;

        ChildOrderIndex = -1;
    }

    public DbChildOrder(string productCode, BfChildOrderStatus order)
    {
        ProductCode = productCode;
        Side = order.Side;
        OrderType = order.ChildOrderType;
        OrderSize = order.Size;

        ChildOrderIndex = -1;

        Update(order);
    }

    public void Update(BfChildOrderStatus order)
    {
        PagingId = order.Id;
        AcceptanceId = order.ChildOrderAcceptanceId;
        OrderId = order.ChildOrderId;
        OrderPrice = order.Price;
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

    public DbChildOrder(BfParentOrder req, BfParentOrderAcceptance resp, int childOrderIndex)
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

    public DbChildOrder(string productCode, BfParentOrderDetailStatus detail, int childOrderIndex)
    {
        ProductCode = productCode;
        Update(detail, childOrderIndex);
    }

    public void Update(BfParentOrderDetailStatus detail, int childOrderIndex)
    {
        OrderType = detail.Parameters[childOrderIndex].ConditionType; // To overwrite limit/market to stop/stop limit/trail
        Side = detail.Parameters[childOrderIndex].Side;
        OrderSize = detail.Parameters[childOrderIndex].Size;
        ExpireDate = detail.ExpireDate;
        TimeInForce = detail.TimeInForce;

        OrderPrice = detail.Parameters[childOrderIndex].Price;
        TriggerPrice = detail.Parameters[childOrderIndex].TriggerPrice;
        Offset = detail.Parameters[childOrderIndex].Offset;

        ParentOrderAcceptanceId = detail.ParentOrderAcceptanceId;
        ParentOrderId = detail.ParentOrderId;
        ChildOrderIndex = childOrderIndex;
    }

#pragma warning disable CS8629
    public void Update(BfParentOrderEvent poe)
    {
        ParentOrderAcceptanceId = poe.ParentOrderAcceptanceId;
        ParentOrderId = poe.ParentOrderId;
        AcceptanceId = poe.ChildOrderAcceptanceId;

        switch (poe.EventType)
        {
            case BfOrderEventType.Trigger:
                ExpireDate = poe.ExpireDate;
                ChildOrderIndex = poe.ChildOrderIndex.Value - 1;
                break;

            case BfOrderEventType.Complete:
                break;
        }
    }
#pragma warning restore CS8629
}
