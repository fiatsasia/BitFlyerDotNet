//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

public class BfxConfiguration
{
    public string DefaultProductCode { get; set; } = BfProductCode.FX_BTC_JPY;

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


    public Dictionary<string, decimal> OrderSizeMax { get; } = new();
    public bool OrderPriceLimitter { get; } = true;

    public BfxConfiguration()
    {
        OrderSizeMax[BfProductCode.FX_BTC_JPY] = BfProductCode.GetMinimumOrderSize(BfProductCode.FX_BTC_JPY);
    }
}
