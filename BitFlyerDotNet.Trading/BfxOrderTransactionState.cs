//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

namespace BitFlyerDotNet.Trading
{
    public enum BfxOrderTransactionState
    {
        Idle,

        SendingOrder,
        WaitingOrderAccepted,

        SendingCancel,
        CancelAccepted,

        Closed,
    }
}
