//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Financial.Extensions;

namespace BitFlyerDotNet.LightningApi
{
    public interface IBfOhlcEx : IFxOhlcvv
    {
        int ExecutionCount { get; }
        double BuyVolume { get; }
        double SellVolume { get; }
        double ExecutedVolume { get; }
    }
}
