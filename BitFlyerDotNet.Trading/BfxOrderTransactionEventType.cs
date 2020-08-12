//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

namespace BitFlyerDotNet.Trading
{
    public enum BfxOrderTransactionEventType
    {
        Unknown,

        OrderSending,
        OrderSent,
        OrderSendFailed,
        OrderSendCanceled,
        Ordered,
        OrderFailed,

        PartiallyExecuted,
        Executed,
        Triggered,
        Completed,

        CancelSending,
        CancelSent,
        CancelSendFailed,
        CancelSendCanceled,
        Canceled,
        CancelFailed,

        Expired,

        ChildOrderEvent,
    }
}
