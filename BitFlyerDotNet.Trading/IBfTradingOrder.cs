//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public enum BfxOrderTransactionEventKind
    {
        OrderRequested,
        OrderAccepted,
        OrderConfirmed,
        OrderFailed,

        CancelRequested,
        CancelAccepted,
        CancelConfirmed,
        Canceled,
        CancelFailed,
        CanceledFromOther,

        OrderKilled,    // When time in force is FOK
        OrderExpired,

        PartiallyExecuted,
        Executed,
    }

    public interface IBfOrderTransaction
    {
        bool IsOrderable();
        object Tag { get; set; }

        bool SendOrder();
        bool CancelOrder();
    }

    public class BfxChildOrderEventArgs : EventArgs
    {
        public BfxChildOrder Order { get; private set; }
        public BfOrderState OrderState => Order.OrderState;

        public BfxChildOrderEventArgs(BfxChildOrder order)
        {
            Order = order;
        }
    }

    public class BfxChildOrderTransactionEventArgs : EventArgs
    {
        public BfxOrderTransactionEventKind Kind { get; private set; }
        public BfxChildOrderTransactionState State { get; private set; }
        public DateTime Time { get; private set; }

        public BfxChildOrderTransactionEventArgs(
            BfxOrderTransactionEventKind kind,
            BfxChildOrderTransactionState state,
            DateTime time
        )
        {
            Kind = kind;
            State = state;
            Time = time;
        }
    }

    public class BfxParentOrderEventArgs : EventArgs
    {
        public BfxParentOrder Order { get; private set; }
        public BfOrderState ParentOrderState => Order.ParentOrderState;

        public BfxParentOrderEventArgs(BfxParentOrder order)
        {
            Order = order;
        }
    }

    public class BfxParentOrderTransactionEventArgs : EventArgs
    {
        public BfxOrderTransactionEventKind Kind { get; private set; }
        public BfxParentOrderTransactionState State { get; private set; }
        public DateTime Time { get; private set; }

        public BfxParentOrderTransactionEventArgs(
            BfxOrderTransactionEventKind kind,
            BfxParentOrderTransactionState state,
            DateTime time
        )
        {
            Kind = kind;
            State = state;
            Time = time;
        }
    }
}
