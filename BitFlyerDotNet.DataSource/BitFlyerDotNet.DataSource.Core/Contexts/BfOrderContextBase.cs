//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.DataSource;

[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public abstract class BfOrderContextBase
{
    #region Index properties
    public string ProductCode { get; }
    public string? OrderAcceptanceId { get; private set; }
    public string? OrderId { get; private set; }
    public DateTime? OrderDate { get; private set; }
    public DateTime? ExpireDate { get; private set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public BfOrderState? OrderState { get; private set; }
    public long? Id { get; protected set; }
    #endregion

    #region Common properties
    [JsonConverter(typeof(StringEnumConverter))]
    public BfOrderType OrderType { get; private set; }
    public int? MinuteToExpire { get; private set; }
    [JsonConverter(typeof(StringEnumConverter))]
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
    [JsonConverter(typeof(StringEnumConverter))]
    public BfTradeSide? Side { get; private set; }
    public decimal? OrderSize { get; private set; }
    public decimal? OrderPrice { get; private set; }
    public decimal? TriggerPrice { get; private set; }
    public decimal? TrailOffset { get; private set; }
    internal List<BfExecutionContext> Executions { get; init; } = new();
    public virtual IReadOnlyList<BfExecutionContext> GetExecutions() => Executions;
    internal BfOrderContextBase Parent { get; private set; }
    public virtual void SetParent(BfOrderContextBase parent) => Parent = parent;
    public bool HasParent => Parent != null;
    #endregion

    #region Parent order ibly properties
    public abstract BfOrderContextBase GetChild(int childIndex);
    public abstract void SetChild(int childIndex, BfOrderContextBase child);
    protected abstract void SetChildrenSize(int count);
    public abstract int ChildCount();
    public virtual IEnumerable<BfOrderContextBase> GetChildren() { for (int i = 0; i < ChildCount(); i++) yield return GetChild(i); }
    #endregion

    [JsonIgnore]
    public bool HasChildren => ChildCount() > 0;
    [JsonIgnore]
    public bool IsActive => !string.IsNullOrEmpty(OrderAcceptanceId) && OrderState.HasValue && OrderState.Value == BfOrderState.Active;

    public BfOrderContextBase()
    {
    }

    public BfOrderContextBase(string productCode)
    {
        ProductCode = productCode;
    }

    public BfOrderContextBase(string productCode, BfOrderType orderType)
    {
        ProductCode = productCode;
        OrderType = orderType;
    }

    public abstract BfOrderContextBase ContextUpdated();

    public BfOrderContextBase OrderAccepted(string acceptanceId)
    {
        OrderAcceptanceId = acceptanceId;
        return this;
    }

    public BfOrderContextBase Update(IBfOrderEvent e) => e switch
    {
        BfChildOrderEvent coe => Update(coe),
        BfParentOrderEvent poe => Update(poe),
        _ => throw new ArgumentException()
    };

    BfOrderType UpdateOrderType(BfOrderType current, BfOrderType update)
    {
        switch (current)
        {
            case BfOrderType.Stop:
            case BfOrderType.StopLimit:
            case BfOrderType.Trail:
                return current;

            default:
                return update;
        }
    }

    decimal? UpdateDecimalNullable(decimal value) => value > 0m ? value : null;
    decimal? UpdateDecimalNullable(decimal? value) => (value.HasValue && value.Value > 0m) ? value.Value : null;
    decimal? UpdatePrice(decimal price) => price == 0m ? null : BfProductCode.FixSizeDecimalPoint(ProductCode, price);
    decimal? UpdatePrice(decimal? price) => (price.HasValue && price.Value == 0m) ? null : BfProductCode.FixSizeDecimalPoint(ProductCode, price.Value);


    BfOrderContextBase Update(BfParentOrderDetailStatusParameter order)
    {
        OrderType = UpdateOrderType(OrderType, order.ConditionType);
        Side = order.Side;
        OrderSize = order.Size;
        if (OrderType == BfOrderType.Limit || OrderType == BfOrderType.StopLimit)
        {
            OrderPrice = UpdatePrice(order.Price);
        }
        if (OrderType == BfOrderType.Stop || OrderType == BfOrderType.StopLimit)
        {
            TriggerPrice = UpdatePrice(order.TriggerPrice);
        }
        if (OrderType == BfOrderType.Trail)
        {
            TrailOffset = UpdatePrice(order.Offset);
        }
        return this;
    }

    public BfOrderContextBase Update(BfParentOrderStatus status, BfParentOrderDetailStatus detail)
    {
        OrderAcceptanceId = status.ParentOrderAcceptanceId;
        Id = status.Id;
        OrderId = status.ParentOrderId;
        OrderType = UpdateOrderType(OrderType, status.ParentOrderType);
        OrderState = status.ParentOrderState;
        ExpireDate = status.ExpireDate;
        OrderDate = status.ParentOrderDate;
        TotalCommission = UpdateDecimalNullable(status.TotalCommission);
        TimeInForce = detail.TimeInForce;

        SetChildrenSize(detail.Parameters.Length);
        for (int index = 0; index < detail.Parameters.Length; index++)
        {
            GetChild(index).Update(detail.Parameters[index]);
        }

        return this;
    }

    BfOrderContextBase Update(BfParentOrderParameter order)
    {
        OrderType = UpdateOrderType(OrderType, order.ConditionType);
        Side = order.Side;
        OrderPrice = UpdatePrice(order.Price);
        OrderSize = order.Size;
        TriggerPrice = UpdatePrice(order.TriggerPrice);
        TrailOffset = UpdatePrice(order.Offset);
        return this;
    }

    public BfOrderContextBase Update(IBfOrder order) => order switch
    {
        BfChildOrder childOrder => Update(childOrder),
        BfParentOrder parentOrder => Update(parentOrder),
        _ => throw new ArgumentException()
    };

    public BfOrderContextBase Update(BfParentOrder order)
    {
        OrderType = UpdateOrderType(OrderType, order.OrderMethod);
        MinuteToExpire = order.MinuteToExpire;
        TimeInForce = order.TimeInForce;

        SetChildrenSize(order.Parameters.Count);
        for (int index = 0; index < order.Parameters.Count; index++)
        {
            GetChild(index).Update(order.Parameters[index]);
        }
        return this;
    }

    BfOrderContextBase Update(BfParentOrderEvent e)
    {
        OrderId = e.ParentOrderId;
        OrderAcceptanceId = e.ParentOrderAcceptanceId;

        switch (e.EventType)
        {
            case BfOrderEventType.Order:
                OrderDate = e.EventDate;
                OrderType = UpdateOrderType(OrderType, e.ParentOrderType.Value);
                ExpireDate = e.ExpireDate;
                OrderState = BfOrderState.Active;
                SetChildrenSize(OrderType.GetChildCount());
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
                    SetChildrenSize(index + 1);
                    var child = GetChild(index);
                    child.OrderDate = e.EventDate;
                    child.OrderType = UpdateOrderType(child.OrderType, e.ChildOrderType.Value);
                    child.OrderAcceptanceId = e.ChildOrderAcceptanceId;
                    child.Side = e.Side;
                    child.OrderPrice = UpdatePrice(e.Price);
                    child.OrderSize = e.Size;
                    child.ExpireDate = e.ExpireDate;
                    child.OrderState = BfOrderState.Active;
                }
                break;

            case BfOrderEventType.Complete: // Complete child
                {
                    var index = e.ChildOrderIndex.Value - 1;
                    SetChildrenSize(index + 1);
                    var child = GetChild(index);
                    child.OrderAcceptanceId = e.ChildOrderAcceptanceId;
                    child.OrderState = BfOrderState.Completed;
                    if (GetChildren().All(c => c.OrderState == BfOrderState.Completed))
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

    public BfOrderContextBase Update(BfChildOrder order)
    {
        OrderType = UpdateOrderType(OrderType, order.ChildOrderType);
        Side = order.Side;
        OrderPrice = UpdatePrice(order.Price);
        OrderSize = order.Size;
        MinuteToExpire = order.MinuteToExpire;
        TimeInForce = order.TimeInForce;

        return this;
    }

    public BfOrderContextBase Update(BfChildOrderStatus status)
    {
        OrderAcceptanceId = status.ChildOrderAcceptanceId;
        Id = status.Id;
        OrderId = status.ChildOrderId;
        Side = status.Side;
        OrderType = UpdateOrderType(OrderType, status.ChildOrderType);
        OrderPrice = UpdatePrice(status.Price);
        AveragePrice = UpdatePrice(status.AveragePrice);
        OrderSize = status.Size;
        OrderState = status.ChildOrderState;
        ExpireDate = status.ExpireDate;
        OrderDate = status.ChildOrderDate;
        OutstandingSize = UpdateDecimalNullable(status.OutstandingSize);
        CancelSize = UpdateDecimalNullable(status.CancelSize);
        ExecutedSize = UpdateDecimalNullable(status.ExecutedSize);
        TotalCommission = UpdateDecimalNullable(status.TotalCommission);

        return this;
    }

    public BfOrderContextBase Update(BfChildOrderStatus status, IEnumerable<BfPrivateExecution> execs)
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

    public BfOrderContextBase UpdateChild(BfChildOrderStatus status, IEnumerable<BfPrivateExecution> execs)
    {
        var children = GetChildren().ToList(); // IReadOnlyList<T> doesn't have FindIndex method. See https://github.com/dotnet/runtime/issues/24227
        var index = children.FindIndex(e => e.OrderAcceptanceId == status.ChildOrderAcceptanceId);
        if (index == -1)
        {
            index = children.FindIndex(e => (CompareChildOrderType(e.OrderType, status.ChildOrderType) && e.Side == status.Side && e.OrderSize == status.Size && e.OrderPrice == status.Price));
        }

        if (index >= 0)
        {
            children[index].Update(status, execs);
        }

        return this;
    }

    public BfOrderContextBase UpdateChild(BfOrderContextBase child)
    {
        var children = GetChildren().ToList(); // IReadOnlyList<T> doesn't have FindIndex method. See https://github.com/dotnet/runtime/issues/24227
        var index = children.FindIndex(e => e.OrderAcceptanceId == child.OrderAcceptanceId);
        if (index == -1)
        {
            index = children.FindIndex(e => (CompareChildOrderType(e.OrderType, child.OrderType) && e.Side == child.Side && e.OrderSize == child.OrderSize && e.OrderPrice == child.OrderPrice));
        }

        if (index >= 0 && !Object.ReferenceEquals(GetChild(index), child))
        {
            var tempChild = GetChild(index);
            child.OrderType = UpdateOrderType(tempChild.OrderType, child.OrderType);
            SetChild(index, child);
            child.SetParent(this);
        }

        return this;
    }

    BfOrderContextBase Update(BfChildOrderEvent ev)
    {
        OrderAcceptanceId = ev.ChildOrderAcceptanceId;
        OrderId = ev.ChildOrderId;

        switch (ev.EventType)
        {
            case BfOrderEventType.Order:
                OrderDate = ev.EventDate;
                OrderType = UpdateOrderType(OrderType, ev.ChildOrderType.Value);
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
