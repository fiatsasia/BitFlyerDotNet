//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;

namespace BitFlyerDotNet.LightningApi
{
    public class BitFlyerClientConfig
    {
        public const int MinuteToExpireDefault = 43200; // 30 days
        public static readonly TimeSpan MinuteToExpireTimeSpanDefault = TimeSpan.FromDays(1); // 30 days
        public const BfTimeInForce TimeInForceDefault = BfTimeInForce.GTC;

        public int MinuteToExpire { get; set; } = MinuteToExpireDefault;
        public BfTimeInForce TimeInForce { get; set; } = TimeInForceDefault;
    }
}
