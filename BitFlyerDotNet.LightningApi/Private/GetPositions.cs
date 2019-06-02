//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfPosition
    {
        [JsonProperty(PropertyName = "product_code")]
        public string ProductCode { get; private set; }

        [JsonProperty(PropertyName = "side")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTradeSide Side { get; private set; }

        [JsonProperty(PropertyName = "price")]
        public decimal Price { get; private set; }

        [JsonProperty(PropertyName = "size")]
        public decimal Size { get; private set; }

        [JsonProperty(PropertyName = "commission")]
        public decimal Commission { get; private set; }

        [JsonProperty(PropertyName = "swap_point_accumulate")]
        public decimal SwapPointAccumulate { get; private set; }

        [JsonProperty(PropertyName = "require_collateral")]
        public decimal RequireCollateral { get; private set; }

        [JsonProperty(PropertyName = "open_date")]
        public DateTime OpenDate { get; private set; }

        [JsonProperty(PropertyName = "leverage")]
        public decimal Leverage { get; private set; }

        [JsonProperty(PropertyName = "pnl")]
        public decimal ProfitAndLoss { get; private set; }

        [JsonProperty(PropertyName = "sfd")]
        public decimal SwapForDifference { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfPosition[]> GetPositions(BfProductCode productCode)
        {
            return PrivateGet<BfPosition[]>(nameof(GetPositions), "product_code=" + productCode.ToEnumString());
        }
    }
}
