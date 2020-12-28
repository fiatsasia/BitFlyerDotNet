//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
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
