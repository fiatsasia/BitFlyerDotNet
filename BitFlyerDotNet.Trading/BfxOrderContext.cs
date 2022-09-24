//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

#pragma warning disable CS8629

namespace BitFlyerDotNet.Trading;

class BfxOrderContext
{
    #region Order informations
    public string ProductCode { get; }
    public BfOrderType OrderType { get; private set; }
    public BfTradeSide? Side { get; private set; }
    public decimal? OrderSize { get; private set; }
    public decimal? OrderPrice { get; private set; }
    public decimal? TriggerPrice { get; private set; }
    public decimal? TrailOffset { get; private set; }
    public int? MinuteToExpire { get; private set; }
    public BfTimeInForce? TimeInForce { get; private set; }
    public List<BfxOrderContext> Children { get; init; } = new();
    #endregion Order informations

    #region Order management info
    public string? OrderAcceptanceId { get; private set; }
    public string? OrderId { get; private set; }
    public DateTime? OrderDate { get; private set; }
    public DateTime? ExpireDate { get; private set; }
    public BfOrderState? OrderState { get; private set; }
    #endregion Order management info

    public long? PagingId { get; protected set; }
    public decimal? AveragePrice { get; protected set; }
    public decimal? OutstandingSize { get; protected set; }
    public decimal? CancelSize { get; protected set; }
    public decimal? ExecutedPrice { get; protected set; }
    public decimal? ExecutedSize { get; protected set; }
    public decimal? TotalCommission { get; protected set; }
    public string? OrderFailedReason { get; protected set; }       // EventType = OrderFailed

    public bool HasChildren => Children.Count > 0;
    public bool IsActive => !string.IsNullOrEmpty(OrderAcceptanceId) && OrderState.HasValue && OrderState.Value == BfOrderState.Active;

    public ConcurrentDictionary<long, BfxExecution> _execs = new();

    public BfxOrderContext()
    {
    }

    public BfxOrderContext(string productCode)
    {
        ProductCode = productCode;
    }

    void ResizeChildren(int count)
    {
        if (Children.Count >= count)
        {
            return;
        }

        while (Children.Count < count)
        {
            Children.Add(new BfxOrderContext(ProductCode));
        }
    }

    public BfxOrderContext OrderAccepted(string acceptanceId)
    {
        OrderAcceptanceId = acceptanceId;
        return this;
    }

    public BfxOrderContext Update(IBfOrderEvent e) => e switch
    {
        BfChildOrderEvent coe => Update(coe),
        BfParentOrderEvent poe => Update(poe),
        _ => throw new ArgumentException()
    };

    BfxOrderContext Update(BfParentOrderDetailStatusParameter order)
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
            TriggerPrice = order.TriggerPrice;
        }
        if (OrderType == BfOrderType.Trail)
        {
            TrailOffset = order.Offset;
        }
        return this;
    }

    public BfxOrderContext Update(BfParentOrderStatus status, BfParentOrderDetailStatus detail)
    {
        OrderAcceptanceId = status.ParentOrderAcceptanceId;
        PagingId = status.Id;
        OrderId = status.ParentOrderId;
        OrderType = status.ParentOrderType;
        OrderState = status.ParentOrderState;
        ExpireDate = status.ExpireDate;
        OrderDate = status.ParentOrderDate;
        TotalCommission = status.TotalCommission;
        TimeInForce = detail.TimeInForce;

        ResizeChildren(detail.Parameters.Length);
        for (int index = 0; index < detail.Parameters.Length; index++)
        {
            Children[index].Update(detail.Parameters[index]);
        }

        return this;
    }

    BfxOrderContext Update(BfParentOrderParameter order)
    {
        OrderType = order.ConditionType;
        Side = order.Side;
        OrderPrice = order.Price;
        OrderSize = order.Size;
        TriggerPrice = order.TriggerPrice;
        TrailOffset = order.Offset;
        return this;
    }

    public BfxOrderContext Update(IBfOrder order, string acceptanceId) => order switch
    {
        BfChildOrder childOrder => Update(childOrder, acceptanceId),
        BfParentOrder parentOrder => Update(parentOrder, acceptanceId),
        _ => throw new ArgumentException()
    };

    public BfxOrderContext Update(BfParentOrder order, string acceptanceId)
    {
        OrderAcceptanceId = acceptanceId;
        OrderType = order.OrderMethod;
        MinuteToExpire = order.MinuteToExpire;
        TimeInForce = order.TimeInForce;

        ResizeChildren(order.Parameters.Count);
        for (int index = 0; index < order.Parameters.Count; index++)
        {
            Children[index].Update(order.Parameters[index]);
        }
        return this;
    }

    BfxOrderContext Update(BfParentOrderEvent e)
    {
        OrderId = e.ParentOrderId;
        OrderAcceptanceId = e.ParentOrderAcceptanceId;

        switch (e.EventType)
        {
            case BfOrderEventType.Order:
                OrderDate = e.EventDate;
                OrderType = e.ParentOrderType.Value;
                ExpireDate = e.ExpireDate;
                OrderState = BfOrderState.Active;
                ResizeChildren(OrderType.GetChildCount());
                break;

            case BfOrderEventType.OrderFailed:
                OrderFailedReason = e.OrderFailedReason;
                OrderState = BfOrderState.Rejected;
                break;

            case BfOrderEventType.Cancel:
                OrderState = BfOrderState.Canceled;
                break;

            case BfOrderEventType.Trigger:
                {
                    var index = e.ChildOrderIndex.Value - 1;
                    ResizeChildren(index + 1);
                    var child = Children[index];
                    child.OrderDate = e.EventDate;
                    child.OrderType = e.ChildOrderType.Value;
                    child.OrderAcceptanceId = e.ChildOrderAcceptanceId;
                    child.Side = e.Side;
                    child.OrderPrice = e.Price > decimal.Zero ? e.Price : null;
                    child.OrderSize = e.Size;
                    child.ExpireDate = e.ExpireDate;
                    child.OrderState = BfOrderState.Active;
                }
                break;

            case BfOrderEventType.Complete: // Complete child
                {
                    var index = e.ChildOrderIndex.Value - 1;
                    ResizeChildren(index + 1);
                    var child = Children[index];
                    child.OrderAcceptanceId = e.ChildOrderAcceptanceId;
                    child.OrderState = BfOrderState.Completed;
                    if (Children.All(c => c.OrderState == BfOrderState.Completed))
                    {
                        OrderState = BfOrderState.Completed;
                    }
                }
                break;

            case BfOrderEventType.Expire:
                OrderState = BfOrderState.Expired;
                break;

            default:
                throw new ArgumentException();
        }

        return this;
    }

    public BfxOrderContext Update(BfChildOrder order, string acceptanceId)
    {
        OrderAcceptanceId = acceptanceId;
        OrderType = order.ChildOrderType;
        Side = order.Side;
        OrderPrice = order.Price;
        OrderSize = order.Size;
        MinuteToExpire = order.MinuteToExpire;
        TimeInForce = order.TimeInForce;
        return this;
    }

    public BfxOrderContext Update(BfChildOrderStatus status, IEnumerable<BfPrivateExecution> execs)
    {
        OrderAcceptanceId = status.ChildOrderAcceptanceId;
        PagingId = status.Id;
        OrderId = status.ChildOrderId;
        Side = status.Side;
        OrderType = status.ChildOrderType;
        OrderPrice = status.Price;
        AveragePrice = status.AveragePrice;
        OrderSize = status.Size;
        OrderState = status.ChildOrderState;
        ExpireDate = status.ExpireDate;
        OrderDate = status.ChildOrderDate;
        OutstandingSize = status.OutstandingSize;
        CancelSize = status.CancelSize;
        ExecutedSize = status.ExecutedSize;
        TotalCommission = status.TotalCommission;

        if (execs != default)
        {
            foreach (var exec in execs)
            {
                _execs.GetOrAdd(exec.Id, _ => new BfxExecution(exec));
            }
        }

        return this;
    }

    bool CompareChildOrderType(BfOrderType ordered, BfOrderType executed)
    {
        if (ordered == executed)
        {
            return true;
        }
        else if (ordered == BfOrderType.StopLimit && executed == BfOrderType.Limit)
        {
            return true;
        }
        else if (ordered == BfOrderType.Stop && executed == BfOrderType.Market)
        {
            return true;
        }
        else if (ordered == BfOrderType.Trail && executed == BfOrderType.Market)
        {
            return true;
        }

        return false;
    }

    public BfxOrderContext UpdateChild(BfChildOrderStatus status, IEnumerable<BfPrivateExecution> execs)
    {
        var index = Children.FindIndex(e => e.OrderAcceptanceId == status.ChildOrderAcceptanceId);
        if (index == -1)
        {
            index = Children.FindIndex(e => (CompareChildOrderType(e.OrderType, status.ChildOrderType) && e.Side == status.Side && e.OrderSize == status.Size && e.OrderPrice == status.Price));
        }

        if (index >= 0)
        {
            Children[index].Update(status, execs);
        }

        return this;
    }

    BfxOrderContext Update(BfChildOrderEvent e)
    {
        OrderAcceptanceId = e.ChildOrderAcceptanceId;
        OrderId = e.ChildOrderId;

        switch (e.EventType)
        {
            case BfOrderEventType.Order:
                OrderDate = e.EventDate;
                OrderType = e.ChildOrderType.Value;
                OrderPrice = e.Price;
                Side = e.Side;
                OrderSize = e.Size;
                ExpireDate = e.ExpireDate;
                OrderState = BfOrderState.Active;
                break;

            case BfOrderEventType.OrderFailed:
                OrderFailedReason = e.OrderFailedReason;
                OrderState = BfOrderState.Rejected;
                break;

            case BfOrderEventType.Cancel:
                OrderState = BfOrderState.Canceled;
                break;

            case BfOrderEventType.CancelFailed:
                OrderState = BfOrderState.Completed;
                break;

            case BfOrderEventType.Execution:
                Side = e.Side;
                _execs.TryAdd(e.ExecutionId.Value, new BfxExecution(e));
                ExecutedSize = _execs.Values.Sum(e => e.Size);
                ExecutedPrice = Math.Round(_execs.Values.Sum(e => e.Price * e.Size) / ExecutedSize.Value, BfProductCode.GetPriceDecimals(ProductCode)); // VWAP
                OrderState = (OrderSize > ExecutedSize) ? BfOrderState.Active : BfOrderState.Completed;
                break;

            case BfOrderEventType.Expire:
                OrderState = BfOrderState.Expired;
                break;

            default:
                throw new ArgumentException();
        };

        return this;
    }
}
