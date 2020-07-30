//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Financier;

namespace BitFlyerDotNet.Historical
{
    public interface IBfOhlc : IOhlcvv<decimal>
    {
        int ExecutionCount { get; }
        double BuyVolume { get; }
        double SellVolume { get; }
        double ExecutedVolume { get; }
    }
}
