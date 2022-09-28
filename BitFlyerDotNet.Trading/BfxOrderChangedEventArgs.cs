//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

public class BfxOrderChangedEventArgs : EventArgs
{
    public BfxOrderEventType EventType { get; internal set; }
    public DateTime Time { get; internal set; }
    public BfxOrder Order { get; }

    public BfxOrderChangedEventArgs(BfxOrderEventType eventType, BfxOrder order)
    {
        EventType = eventType;
        Time = DateTime.UtcNow;
        Order = order;
    }

    internal BfxOrderChangedEventArgs(BfxOrderEventType eventType, BdOrderContext status)
        : this(eventType, new BfxOrder(status))
    {
    }

    internal BfxOrderChangedEventArgs(IBfOrderEvent e, BdOrderContext status)
        : this(
            e switch
            {
                BfChildOrderEvent coe => coe.EventType switch
                {
                    BfOrderEventType.Order => BfxOrderEventType.OrderConfirmed,
                    BfOrderEventType.OrderFailed => BfxOrderEventType.OrderFailed,
                    BfOrderEventType.Cancel => BfxOrderEventType.OrderCanceled,
                    BfOrderEventType.CancelFailed => BfxOrderEventType.CancelFailed,
                    BfOrderEventType.Execution => (status.OrderSize > status.ExecutedSize)
                        ? BfxOrderEventType.PartiallyExecuted
                        : BfxOrderEventType.Executed,
                    BfOrderEventType.Expire => BfxOrderEventType.Expired,
                    _ => throw new ArgumentException()
                },
                BfParentOrderEvent poe => poe.EventType switch
                {
                    BfOrderEventType.Order => BfxOrderEventType.OrderConfirmed,
                    BfOrderEventType.OrderFailed => BfxOrderEventType.OrderFailed,
                    BfOrderEventType.Cancel => BfxOrderEventType.OrderCanceled,
                    BfOrderEventType.Trigger => BfxOrderEventType.ChildOrderChanged,
                    BfOrderEventType.Complete => BfxOrderEventType.ChildOrderChanged,
                    BfOrderEventType.Expire => BfxOrderEventType.Expired,
                    _ => throw new ArgumentException()
                },
                _ => throw new ArgumentException()
            },
            new BfxOrder(status)
        )
    {
    }
}
