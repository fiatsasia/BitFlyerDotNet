//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Text;

namespace BitFlyerDotNet.Trading
{
    public enum BfxOrderEventType
    {
        Unknown,

        OrderSent,
        OrderSendFailed,
        OrderAccepted,
        OrderFailed,

        CancelSent,
        CancelSendFailed,
        Canceled,
        CancelFailed,

        PartiallyExecuted,
        Executed,

        Completed,
        Expired,
    }
}
