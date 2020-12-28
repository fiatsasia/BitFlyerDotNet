//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
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
        public IBfxOrder ChildOrder => Order.Children[ChildOrderIndex];

        public BfxOrderTransactionEventArgs(IBfxOrder order)
        {
            Order = order;
        }
    }
}
