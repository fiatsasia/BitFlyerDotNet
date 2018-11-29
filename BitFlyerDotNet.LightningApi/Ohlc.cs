//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;

namespace BitFlyerDotNet.LightningApi
{
    public interface IBfOhlc
    {
        DateTime Start { get; }
        double Open { get; }
        double High { get; }
        double Low { get; }
        double Close { get; }
        double Volume { get; }
    }

    public interface IBfOhlcEx : IBfOhlc
    {
        int TickCount { get; }
        double VWAP { get; }
        double BuyVolume { get; }
        double SellVolume { get; }
        double ExecutedVolume { get; }
    }
}
