//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

public enum BfxOrderEventType
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
