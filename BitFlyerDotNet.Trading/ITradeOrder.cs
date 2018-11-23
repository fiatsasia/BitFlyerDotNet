//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public enum OrderTransactionState
    {
        Created,
        Ordering,
        OrderAccepted,
        OrderFailed,
        OrderConfirmed,
        Executing,
        Executed,
        Canceling,
        CancelAccepted,
        CancelFailed,
        Canceled,
        CancelIgnored,
        Expired,
        Rejected,
    }

    internal static class OrderTransactionStateMixIn
    {
        public static bool IsOrderable(this OrderTransactionState state)
        {
            switch (state)
            {
                case OrderTransactionState.Created:
                case OrderTransactionState.OrderFailed:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsOrdered(this OrderTransactionState state)
        {
            switch (state)
            {
                case OrderTransactionState.Created:
                case OrderTransactionState.Ordering:
                case OrderTransactionState.OrderAccepted:
                case OrderTransactionState.OrderFailed:
                    return false;

                default:
                    return true;
            }
        }

        public static bool IsCompleted(this OrderTransactionState state)
        {
            switch (state)
            {
                case OrderTransactionState.Executed:
                case OrderTransactionState.Canceled:
                case OrderTransactionState.CancelIgnored:
                case OrderTransactionState.Expired:
                case OrderTransactionState.Rejected:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsCancelable(this OrderTransactionState state)
        {
            switch (state)
            {
                case OrderTransactionState.OrderFailed:
                case OrderTransactionState.OrderAccepted:
                case OrderTransactionState.OrderConfirmed:
                case OrderTransactionState.Executing:
                case OrderTransactionState.CancelFailed:
                    return true;

                default:
                    return false;
            }
        }
    }

    public delegate void OrderTransactionStatusChangedCallback(OrderTransactionState status, IOrderTransaction transaction);
    public delegate void PositionStatusChangedCallback(BfPosition position, bool openedOrClosed);

    public interface IChildOrder
    {
        BfProductCode ProductCode { get; }
        BfOrderType OrderType { get; }
        BfTradeSide Side { get; }
        double OrderSize { get; }
        double OrderPrice { get; }
        double StopTriggerPrice { get; }
        double TrailingStopPriceOffset { get; }
    }

    public interface IParentOrder
    {
        BfProductCode ProductCode { get; }
        BfOrderType OrderType { get; }
        IChildOrder[] ChildOrders { get; }
    }

    public interface IOrderTransaction
    {
        DateTime OrderDate { get; }
        DateTime OrderCreatedTime { get; }
        DateTime OrderRequestedTime { get; }
        DateTime OrderAcceptedTime { get; }
        double ReferencePrice { get; }
        BfOrderState OrderStatus { get; }

        int MinuteToExpire { get; }
        BfTimeInForce TimeInForce { get; }

        DateTime ExecutedTime { get; }

        bool IsError { get; }
        bool IsExecuted { get; }
        bool IsCompleted { get; }
        bool IsCancelable { get; }

        object Tag { get; set; }

        OrderTransactionState TransactionStatus { get; }
        event OrderTransactionStatusChangedCallback StatusChanged;

        bool Send();
        bool Confirm();
        bool Cancel();
    }

    public interface IChildOrderTransaction : IChildOrder, IOrderTransaction
    {
        string ChildOrderAcceptanceId { get; }
        string ChildOrderId { get; }
        double ExecutedPrice { get; }
        double ExecutedSize { get; }
    }

    public interface IParentOrderTransaction : IParentOrder, IOrderTransaction
    {
        string ParentOrderAcceptanceId { get; }
        string ParentOrderId { get; }
        List<IBfExecution> Executions { get; }
    }
}
