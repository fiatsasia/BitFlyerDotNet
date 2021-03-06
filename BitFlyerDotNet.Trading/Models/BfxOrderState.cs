﻿//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading
{
    public enum BfxOrderState
    {
        Outstanding,

        Ordered,
        OrderFailed,

        PartiallyExecuted,
        Executed,

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
                case BfxOrderState.OrderFailed:
                case BfxOrderState.Executed:
                case BfxOrderState.Canceled:
                case BfxOrderState.CancelFailed:
                case BfxOrderState.Completed:
                case BfxOrderState.Expired:
                    return true;

                default:
                    return false;
            }
        }
    }
}
