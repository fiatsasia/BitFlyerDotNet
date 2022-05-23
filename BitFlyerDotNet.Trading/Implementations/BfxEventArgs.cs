//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;

namespace BitFlyerDotNet.Trading
{
    public class BfxPositionChangedEventArgs : EventArgs
    {
        public BfxPositionEventType EventType { get; internal set; }
        public DateTime Time { get; internal set; }
        public BfxPosition Position { get; internal set; }

        public BfxPositionChangedEventArgs(DateTime time, BfxPosition pos)
        {
            Time = time;
            Position = pos;
        }
    }

    public class BfxOrderChangedEventArgs : EventArgs
    {
        public BfxOrderEventType EventType { get; internal set; }
        public DateTime Time { get; internal set; }
        public BfxTransactionState State { get; internal set; }
        public BfxOrderState OrderState { get; internal set; }

        public IBfxOrder Order { get; }
        public object? Parameter { get; set; }

        public BfxOrderEventType ChildEventType { get; internal set; }
        public int ChildOrderIndex { get; internal set; }
        public IBfxOrder ChildOrder => Order.Children[ChildOrderIndex];

        public BfxOrderChangedEventArgs(IBfxOrder order)
        {
            Order = order;
        }
    }
}
