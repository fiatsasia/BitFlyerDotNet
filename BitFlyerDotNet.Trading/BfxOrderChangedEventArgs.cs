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
    public BfxOrderChangedEventArgs(BfxOrderEventType eventType, BfxOrderStatus status)
    {
        EventType = eventType;
        Order = new BfxOrder(status);
    }
}
