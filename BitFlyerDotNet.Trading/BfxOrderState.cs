//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public enum BfxOrderTransactionState
    {
        Idle,

        SendingOrder,
        WaitingOrderAccepted,
        OrderAccepted,

        SendingCancel,
        WaitingCancelCompleted,
        CancelAccepted,
    }

    public enum BfxOrderState
    {
        Unknown,

        Outstanding,

        Ordering,
        Ordered,
        OrderFailed,

        Executing,
        Executed,

        Canceling,
        Canceled,
        CancelFailed,

        Expired,
    }
}
