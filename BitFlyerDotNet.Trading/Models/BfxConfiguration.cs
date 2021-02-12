//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
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
