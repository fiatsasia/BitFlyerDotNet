//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public enum BfxOrderState
    {
        Unknown,

        // Progress states
        Ordering,
        Ordered,
        OrderConfirmed,
        Executing,

        // Steady states
        Canceled,
        Executed,
        Expired,
        OrderFailed,

        SendingOrder,
        WaitingOrderAccepted,
        SendingCancel,
        WaitingCancelCompleted,

#if false
        // BfOrderState
        Active,
        Completed,
        Canceled,
        Expired,
        Rejected,

        // BfOrderEventType
        Order,
        OrderFailed,
        Cancel,
        CancelFailed,
        Execution,
        Trigger,
        Complete,
        Expire,
#endif
    }

    public static class BfOrderStateExtension
    {
        public static BfxOrderState TransitState(this BfxOrderState currentState, BfxOrderState newState)
        {
            switch (currentState)
            {
                case BfxOrderState.Unknown:
                    switch (newState)
                    {
                        case BfxOrderState.SendingOrder: break;
                        default: throw new ArgumentException();
                    }
                    break;

                case BfxOrderState.OrderConfirmed:
                    switch (newState)
                    {
                        case BfxOrderState.SendingCancel: break;
                        default: throw new ArgumentException();
                    }
                    break;

                case BfxOrderState.SendingOrder:
                    switch (newState)
                    {
                        case BfxOrderState.Unknown: break;              // Send failed
                        case BfxOrderState.WaitingOrderAccepted: break; // Send succeeded
                        default: throw new ArgumentException();
                    }
                    break;

                case BfxOrderState.SendingCancel:
                    switch (newState)
                    {
                        case BfxOrderState.OrderConfirmed: break;              // Send failed
                        case BfxOrderState.WaitingCancelCompleted: break; // Send succeeded
                        default: throw new ArgumentException();
                    }
                    break;

                default:
                    throw new ArgumentException();
            }
            return newState;
        }

        public static BfxOrderState TransitState(this BfxOrderState state, BfOrderEventType evt)
        {
            switch (state)
            {
                case BfxOrderState.Unknown:
                    throw new ArgumentException();

                case BfxOrderState.WaitingOrderAccepted:
                    switch (evt)
                    {
                        case BfOrderEventType.Order: return BfxOrderState.OrderConfirmed;
                        case BfOrderEventType.OrderFailed: return BfxOrderState.Unknown;

                        default: throw new ArgumentException();
                    }

                case BfxOrderState.OrderConfirmed:
                    switch (evt)
                    {
                        case BfOrderEventType.Execution: return BfxOrderState.OrderConfirmed;
                        case BfOrderEventType.Complete: return BfxOrderState.Executed;
                        case BfOrderEventType.Expire: return BfxOrderState.Expired;
                        default: throw new ArgumentException();
                    }

                case BfxOrderState.WaitingCancelCompleted:
                    switch (evt)
                    {
                        case BfOrderEventType.Cancel: return BfxOrderState.Canceled;
                        case BfOrderEventType.CancelFailed: return BfxOrderState.Unknown;
                        default: throw new ArgumentException();
                    }

                case BfxOrderState.SendingOrder:
                case BfxOrderState.SendingCancel:
                case BfxOrderState.Canceled:
                case BfxOrderState.Executed:
                case BfxOrderState.Expired:
                default:
                    throw new ArgumentException();
            }
        }

        public static BfxOrderState GetOrderDetailState(this BfOrderState state, BfOrderEventType evt)
        {
            switch (state)
            {
                case BfOrderState.Unknown:
                    switch (evt)
                    {
                        case BfOrderEventType.Order:        return BfxOrderState.OrderConfirmed;
                        case BfOrderEventType.OrderFailed:  return BfxOrderState.Unknown;
                        case BfOrderEventType.Cancel:       return BfxOrderState.Canceled;
                        case BfOrderEventType.CancelFailed: return BfxOrderState.Unknown;
                        case BfOrderEventType.Execution:    return BfxOrderState.OrderConfirmed;
                        case BfOrderEventType.Trigger:      return BfxOrderState.OrderConfirmed; // Parent child only
                        case BfOrderEventType.Complete:     return BfxOrderState.Executed;
                        case BfOrderEventType.Expire:       return BfxOrderState.Expired;
                        default:                            throw new ArgumentException();
                    }

                case BfOrderState.Active:
                    switch (evt)
                    {
                        case BfOrderEventType.Cancel:       return BfxOrderState.Canceled;
                        case BfOrderEventType.CancelFailed: return BfxOrderState.Unknown;
                        case BfOrderEventType.Execution:    return BfxOrderState.OrderConfirmed;
                        case BfOrderEventType.Complete:     return BfxOrderState.Executed;
                        case BfOrderEventType.Expire:       return BfxOrderState.Expired;
                        default:                            throw new ArgumentException();
                    }

                case BfOrderState.Completed:
                case BfOrderState.Canceled:
                case BfOrderState.Expired:
                case BfOrderState.Rejected:
                default:
                    throw new ArgumentException();
            }
        }
    }
}
