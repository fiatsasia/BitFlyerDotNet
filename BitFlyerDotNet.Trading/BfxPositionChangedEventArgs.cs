//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

public class BfxPositionChangedEventArgs : EventArgs
{
    public BfxPositionEventType EventType { get; internal set; }
    public DateTime Time { get; internal set; }
    public BfxPosition Position { get; internal set; }

    public BfxPositionChangedEventArgs(BfxPosition pos)
    {
        if (pos.IsOpened)
        {
            EventType = BfxPositionEventType.Opened;
            Time = pos.OpenTime;
        }
        else
        {
            EventType = BfxPositionEventType.Closed;
#pragma warning disable CS8629
            Time = pos.CloseTime.Value;
#pragma warning restore CS8629
        }

        Position = pos;
    }
}
