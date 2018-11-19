//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using Newtonsoft.Json;

namespace BitFlyerDotNet.LightningApi
{
    public class BfCollateral
    {
        [JsonProperty(PropertyName = "collateral")]
        public double Collateral { get; private set; }

        [JsonProperty(PropertyName = "open_position_pnl")]
        public double OpenPositionProfitAndLoss { get; private set; }

        [JsonProperty(PropertyName = "require_collateral")]
        public double RequireCollateral { get; private set; }

        [JsonProperty(PropertyName = "keep_rate")]
        public double KeepRate { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfCollateral> GetCollateral()
        {
            return PrivateGet<BfCollateral>(nameof(GetCollateral));
        }
    }
}
