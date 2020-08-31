//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;

namespace BitFlyerDotNet.Trading
{
    public class BfxEventArgs : EventArgs
    {
        public DateTime Time { get; internal set; }
    }

    public class BfxPositionEventArgs : BfxEventArgs
    {
        public BfxPositionEventType EventType { get; internal set; }
        public BfxPosition Position { get; internal set; }

        public BfxPositionEventArgs(DateTime time, BfxPosition pos)
        {
            Time = time;
            Position = pos;
        }
    }

    public class BfxOrderTransactionEventArgs : BfxEventArgs
    {
        public BfxOrderTransactionEventType EventType { get; internal set; }
        public BfxOrderTransactionState State { get; internal set; }
        public BfxOrderState OrderState { get; internal set; }

        public IBfxOrder Order { get; }
        public object? Parameter { get; set; }

        public BfxOrderTransactionEventType ChildEventType { get; internal set; }
        public int ChildOrderIndex { get; internal set; }

        public BfxOrderTransactionEventArgs(IBfxOrder order)
        {
            Order = order;
        }
    }
}
