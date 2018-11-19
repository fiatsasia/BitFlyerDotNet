//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public enum BfTradeOrderState
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

    public enum BfTradeOrderEvent
    {

    }

    internal static class BfTradeOrderStateMixIn
    {
        public static bool IsOrderable(this BfTradeOrderState state)
        {
            switch (state)
            {
                case BfTradeOrderState.Created:
                case BfTradeOrderState.OrderFailed:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsOrdered(this BfTradeOrderState state)
        {
            switch (state)
            {
                case BfTradeOrderState.Created:
                case BfTradeOrderState.Ordering:
                case BfTradeOrderState.OrderAccepted:
                case BfTradeOrderState.OrderFailed:
                    return false;

                default:
                    return true;
            }
        }

        public static bool IsCompleted(this BfTradeOrderState state)
        {
            switch (state)
            {
                case BfTradeOrderState.Executed:
                case BfTradeOrderState.Canceled:
                case BfTradeOrderState.CancelIgnored:
                case BfTradeOrderState.Expired:
                case BfTradeOrderState.Rejected:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsCancelable(this BfTradeOrderState state)
        {
            switch (state)
            {
                case BfTradeOrderState.OrderFailed:
                case BfTradeOrderState.OrderAccepted:
                case BfTradeOrderState.OrderConfirmed:
                case BfTradeOrderState.Executing:
                case BfTradeOrderState.CancelFailed:
                    return true;

                default:
                    return false;
            }
        }
    }

    public delegate void OrderStatusChangedCallback(BfTradeOrderState status, IBfTradeOrder order);
    public delegate void PositionStatusChangedCallback(BfPosition position, bool openedOrClosed);

    public interface IBfTradeOrder
    {
        bool IsSimple { get; }
        BfProductCode ProductCode { get; }
        BfOrderType OrderType { get; }
        BfTradeSide Side { get; }

        DateTime OrderCreatedTime { get; }
        DateTime OrderRequestedTime { get; }
        DateTime OrderAcceptedTime { get; }
        double OrderSize { get; }
        double OrderPrice { get; }
        double ReferencePrice { get; }

        double ExecutedPrice { get; }
        double ExecutedSize { get; }
        DateTime ExecutedTime { get; }

        double TriggerPrice { get; }
        double LimitOffset { get; }

        bool Send(BitFlyerClient client);
        bool Confirm(BitFlyerClient client);
        bool Cancel(BitFlyerClient client);

        bool IsExecuted { get; }
        bool IsCompleted { get; }
        bool IsCancelable { get; }

        bool IsError { get; }
        BfTradeOrderState Status { get; }

        event OrderStatusChangedCallback StatusChanged;

        object Tag { get; set; }
    }
}
