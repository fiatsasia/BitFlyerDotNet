﻿//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

public enum BfxOrderEventType
{
    Unknown,
    OrderAccepted,
    RetryingOrder,
    RetriedOut,
    OrderConfirmed,
    PartiallyExecuted,
    Executed,
    OrderFailed,
    OrderCanceled,
    CancelFailed,
    Expired,
    ChildOrderChanged,
    CancelAccepted,
    CancelRejected,
    CancelIgnored,
}
