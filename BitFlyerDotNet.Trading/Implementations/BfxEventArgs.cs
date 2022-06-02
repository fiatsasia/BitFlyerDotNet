//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using BitFlyerDotNet.LightningApi;

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

    public class BfxTradeChangedEventArgs : EventArgs
    {
        public BfxOrderEventType EventType { get; internal set; }
        public DateTime Time { get; internal set; }
        public BfxTransactionState State { get; internal set; }
        public BfOrderState OrderState { get; internal set; }

        public BfxTrade Order { get; }
        public object? Parameter { get; set; }

        public BfxOrderEventType ChildEventType { get; internal set; }
    }

    public class BfxTransactionChangedEventArgs : EventArgs
    {
        public BfxTransactionEventType EvenetType { get; }
    }
}
