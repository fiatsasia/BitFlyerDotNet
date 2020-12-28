//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
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
            return GetPrivateAsync<BfCollateral>(nameof(GetCollateral)).Result;
        }
    }
}
