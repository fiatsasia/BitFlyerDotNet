//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Newtonsoft.Json;

namespace BitFlyerDotNet.LightningApi
{
    public class BfCollateral
    {
        [JsonProperty(PropertyName = "collateral")]
        public decimal Collateral { get; private set; }

        [JsonProperty(PropertyName = "open_position_pnl")]
        public decimal OpenPositionProfitAndLoss { get; private set; }

        [JsonProperty(PropertyName = "require_collateral")]
        public decimal RequireCollateral { get; private set; }

        [JsonProperty(PropertyName = "keep_rate")]
        public decimal KeepRate { get; private set; }
    }

    /// <summary>
    /// Get Margin Status
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetCollateral">Online help</see>
    /// </summary>
    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfCollateral> GetCollateral()
        {
            return PrivateGet<BfCollateral>(nameof(GetCollateral));
        }
    }
}
