//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfOrderContext
{
    #region Index properties
    public string ProductCode { get; }
    public string? OrderAcceptanceId { get; private set; }
    public string? OrderId { get; private set; }
    public DateTime? OrderDate { get; private set; }
    public DateTime? ExpireDate { get; private set; }
    public BfOrderState? OrderState { get; private set; }
    public long? Id { get; protected set; }
    #endregion

    #region Common properties
    public BfOrderType OrderType { get; private set; }
    public int? MinuteToExpire { get; private set; }
    public BfTimeInForce? TimeInForce { get; private set; }
    public decimal? AveragePrice { get; protected set; }
    public decimal? OutstandingSize { get; protected set; }
    public decimal? CancelSize { get; protected set; }
    public decimal? ExecutedPrice { get; protected set; }
    public decimal? ExecutedSize { get; protected set; }
    public decimal? TotalCommission { get; protected set; }
    public string? OrderFailedReason { get; protected set; }       // EventType = OrderFailed
    #endregion

    #region Child order only properties
    public BfTradeSide? Side { get; private set; }
    public decimal? OrderSize { get; private set; }
    public decimal? OrderPrice { get; private set; }
    public decimal? TriggerPrice { get; private set; }
    public decimal? TrailOffset { get; private set; }
    internal List<BfExecutionContext> Executions { get; init; } = new();
    public virtual IReadOnlyList<BfExecutionContext> GetExecutions() => Executions;
    internal BfOrderContext Parent { get; private set; }
    public bool HasParent => Parent != null;
    #endregion

    #region Parent order ibly properties
    static readonly List<BfOrderContext> EmptyChildren = new();
    internal List<BfOrderContext> Children { get; private set; }
    public virtual IReadOnlyList<BfOrderContext> GetChildren() => Children != default ? Children : EmptyChildren;
    #endregion

    public bool HasChildren => Children.Count > 0;
    public bool IsActive => !string.IsNullOrEmpty(OrderAcceptanceId) && OrderState.HasValue && OrderState.Value == BfOrderState.Active;

    BfPrivateDataSource _ds;

    public BfOrderContext()
    {
    }

    public BfOrderContext(BfPrivateDataSource ds, string productCode)
    {
        _ds = ds;
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
            Children.Add(new BfOrderContext(_ds, ProductCode));
        }
    }

    public virtual BfOrderContext ContextUpdated()
    {
        return _ds.Upsert(this);
    }

    public BfOrderContext OrderAccepted(string acceptanceId)
    {
        OrderAcceptanceId = acceptanceId;
        return this;
    }

    public BfOrderContext Update(IBfOrderEvent e) => e switch
    {
        BfChildOrderEvent coe => Update(coe),
        BfParentOrderEvent poe => Update(poe),
        _ => throw new ArgumentException()
    };

    BfOrderContext Update(BfParentOrderDetailStatusParameter order)
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

    public BfOrderContext Update(BfParentOrderStatus status, BfParentOrderDetailStatus detail)
    {
        OrderAcceptanceId = status.ParentOrderAcceptanceId;
        Id = status.Id;
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

    BfOrderContext Update(BfParentOrderParameter order)
    {
        OrderType = order.ConditionType;
        Side = order.Side;
        OrderPrice = order.Price;
        OrderSize = order.Size;
        TriggerPrice = order.TriggerPrice;
        TrailOffset = order.Offset;
        return this;
    }

    public BfOrderContext Update(IBfOrder order) => order switch
    {
        BfChildOrder childOrder => Update(childOrder),
        BfParentOrder parentOrder => Update(parentOrder),
        _ => throw new ArgumentException()
    };

    public BfOrderContext Update(BfParentOrder order)
    {
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

    BfOrderContext Update(BfParentOrderEvent e)
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

    public BfOrderContext Update(BfChildOrder order)
    {
        OrderType = order.ChildOrderType;
        Side = order.Side;
        OrderPrice = order.Price;
        OrderSize = order.Size;
        MinuteToExpire = order.MinuteToExpire;
        TimeInForce = order.TimeInForce;

        return this;
    }

    public BfOrderContext Update(BfChildOrderStatus status)
    {
        OrderAcceptanceId = status.ChildOrderAcceptanceId;
        Id = status.Id;
        OrderId = status.ChildOrderId;
        Side = status.Side;
        OrderType = status.ChildOrderType;
        OrderPrice = status.Price > 0m ? status.Price : default;
        AveragePrice = status.AveragePrice > 0m ? status.AveragePrice : default;
        OrderSize = status.Size > 0m ? status.Size : default;
        OrderState = status.ChildOrderState;
        ExpireDate = status.ExpireDate;
        OrderDate = status.ChildOrderDate;
        OutstandingSize = status.OutstandingSize > 0m ? status.OutstandingSize : default;
        CancelSize = status.CancelSize > 0m ? status.CancelSize : default;
        ExecutedSize = status.ExecutedSize > 0m ? status.ExecutedSize : default;
        TotalCommission = status.TotalCommission > 0m ? status.TotalCommission : default;

        return this;
    }

    public BfOrderContext Update(BfChildOrderStatus status, IEnumerable<BfPrivateExecution> execs)
    {
        Update(status);
        if (execs != default)
        {
            foreach (var exec in execs)
            {
                var ctx = Executions.FirstOrDefault(e => e.Id == exec.Id);
                if (ctx == null)
                {
                    ctx = new();
                    Executions.Add(ctx);
                }
                ctx.Update(exec);
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

    public BfOrderContext UpdateChild(BfChildOrderStatus status, IEnumerable<BfPrivateExecution> execs)
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

    BfOrderContext Update(BfChildOrderEvent ev)
    {
        OrderAcceptanceId = ev.ChildOrderAcceptanceId;
        OrderId = ev.ChildOrderId;

        switch (ev.EventType)
        {
            case BfOrderEventType.Order:
                OrderDate = ev.EventDate;
                OrderType = ev.ChildOrderType.Value;
                OrderPrice = ev.Price;
                Side = ev.Side;
                OrderSize = ev.Size;
                ExpireDate = ev.ExpireDate;
                OrderState = BfOrderState.Active;
                break;

            case BfOrderEventType.OrderFailed:
                OrderFailedReason = ev.OrderFailedReason;
                OrderState = BfOrderState.Rejected;
                break;

            case BfOrderEventType.Cancel:
                OrderState = BfOrderState.Canceled;
                break;

            case BfOrderEventType.CancelFailed:
                OrderState = BfOrderState.Completed;
                break;

            case BfOrderEventType.Execution:
                {
                    var exec = Executions.FirstOrDefault(e => e.Id == ev.ExecutionId.Value);
                    if (exec == default)
                    {
                        exec = new();
                        Executions.Add(exec);
                    }
                    exec.Update(ev);
                    Side = ev.Side;
                    ExecutedSize = Executions.Sum(e => e.Size);
                    ExecutedPrice = Math.Round(Executions.Sum(e => e.Price * e.Size) / ExecutedSize.Value, BfProductCode.GetPriceDecimals(ProductCode)); // VWAP
                    OrderState = (OrderSize > ExecutedSize) ? BfOrderState.Active : BfOrderState.Completed;
                }
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
