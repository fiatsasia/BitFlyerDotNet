//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;

namespace BitFlyerDotNet.Historical
{
    public interface IOhlcvv
    {
        DateTime Start { get; }
        decimal Open { get; }
        decimal High { get; }
        decimal Low { get; }
        decimal Close { get; }
        double Volume { get; }
        double VWAP { get; }
    }

    public interface IBfOhlc : IOhlcvv
    {
        int ExecutionCount { get; }
        double BuyVolume { get; }
        double SellVolume { get; }
        double ExecutedVolume { get; }
    }
}
