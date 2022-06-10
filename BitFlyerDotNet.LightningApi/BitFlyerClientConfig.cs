//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BitFlyerClientConfig
{
    public const int MinuteToExpireDefault = 43200; // 30 days
    public const BfTimeInForce TimeInForceDefault = BfTimeInForce.GTC;

    public int MinuteToExpire { get; set; } = 0;
    public BfTimeInForce TimeInForce { get; set; } = BfTimeInForce.NotSpecified;
}
