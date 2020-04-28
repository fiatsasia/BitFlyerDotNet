//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Financial.Extensions;

namespace BitFlyerDotNet.LightningApi
{
    public interface IBfOhlc : IOhlcvv<decimal>
    {
        int ExecutionCount { get; }
        double BuyVolume { get; }
        double SellVolume { get; }
        double ExecutedVolume { get; }
    }
}
