//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;

namespace BitFlyerDotNet.Trading
{
    public class BfxPositionChangedEventArgs : EventArgs
    {
        public BfxPositionChange Position { get; internal set; }
        public bool IsOpened => Position.Close == null;

        public BfxPositionChangedEventArgs(BfxPositionChange pos)
        {
            Position = pos;
        }
    }

    public class BfxOrderTransactionEventArgs : EventArgs
    {
        public BfxOrderTransactionEventType EventType { get; internal set; }
        public BfxOrderTransactionState State { get; internal set; }
        public BfxOrderState OrderState { get; internal set; }
        public DateTime Time { get; internal set; }

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
