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

        Completed,
        Expired,
    }

    public static class BfxOrderStateExtensions
    {
        public static bool IsCompleted(this BfxOrderState state)
        {
            switch (state)
            {
                case BfxOrderState.Ordered:
                case BfxOrderState.OrderFailed:
                case BfxOrderState.Executed:
                case BfxOrderState.Canceled:
                case BfxOrderState.CancelFailed:
                case BfxOrderState.Expired:
                    return true;

                default:
                    return false;
            }
        }
    }
}
