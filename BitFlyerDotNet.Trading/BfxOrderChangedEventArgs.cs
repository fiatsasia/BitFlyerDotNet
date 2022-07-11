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

    public BfxOrderChangedEventArgs(BfxOrderEventType eventType, BfxOrderContext status)
    {
        EventType = eventType;
        Order = new BfxOrder(status);
    }

    public BfxOrderChangedEventArgs(BfChildOrderEvent e, BfxOrderContext status)
    {
        EventType = e.EventType switch
        {
            BfOrderEventType.Order => BfxOrderEventType.OrderAccepted,
            BfOrderEventType.OrderFailed => BfxOrderEventType.OrderFailed,
            BfOrderEventType.Cancel => BfxOrderEventType.Canceled,
            BfOrderEventType.CancelFailed => BfxOrderEventType.CancelFailed,
            BfOrderEventType.Execution => (status.OrderSize > status.ExecutedSize)
                ? BfxOrderEventType.PartiallyExecuted
                : BfxOrderEventType.Executed,
            BfOrderEventType.Expire => BfxOrderEventType.Expired,
            _ => throw new ArgumentException()
        };
        Time = e.EventDate;
        Order = new BfxOrder(status);
    }

    public BfxOrderChangedEventArgs(BfParentOrderEvent e, BfxOrderContext status)
    {
        EventType = e.EventType switch
        {
            BfOrderEventType.Order => BfxOrderEventType.OrderAccepted,
            BfOrderEventType.OrderFailed => BfxOrderEventType.OrderFailed,
            BfOrderEventType.Cancel => BfxOrderEventType.Canceled,
            BfOrderEventType.Trigger => BfxOrderEventType.ChildOrderChanged,
            BfOrderEventType.Complete => BfxOrderEventType.ChildOrderChanged,
            BfOrderEventType.Expire => BfxOrderEventType.Expired,
            _ => throw new ArgumentException()
        };
        Time = e.EventDate;
        Order = new BfxOrder(status);
    }
}
