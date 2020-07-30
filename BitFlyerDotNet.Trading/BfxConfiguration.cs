//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;

namespace BitFlyerDotNet.Trading
{
    public class BfxConfiguration
    {
        public TimeSpan PositionUpdateInterval { get; set; } = TimeSpan.FromSeconds(3);
        public int OrderRetryMax { get; set; } = 3;
        public TimeSpan OrderRetryInterval { get; set; } = TimeSpan.FromSeconds(3);

        public TimeSpan ChildOrderConfirmDelay { get; set; } = TimeSpan.FromSeconds(3);
        public TimeSpan ChildOrderConfirmInterval { get; set; } = TimeSpan.FromSeconds(3);

        public TimeSpan ParentOrderConfirmDelay { get; set; } = TimeSpan.FromMilliseconds(500);
        public TimeSpan ParentOrderConfirmInterval { get; set; } = TimeSpan.FromSeconds(3);

        public TimeSpan MarketStatusConfirmInterval { get; set; } = TimeSpan.FromSeconds(5);

        public int CancelRetryMax { get; set; } = 3;
        public TimeSpan CancelRetryInterval { get; set; } = TimeSpan.FromSeconds(3);
    }
}
