//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Fiats.Utils;

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
        public double Price { get; private set; }

        [JsonProperty(PropertyName = "size")]
        public double Size { get; private set; }

        [JsonProperty(PropertyName = "commission")]
        public double Commission { get; private set; }

        [JsonProperty(PropertyName = "swap_point_accumulate")]
        public double SwapPointAccumulate { get; private set; }

        [JsonProperty(PropertyName = "require_collateral")]
        public double RequireCollateral { get; private set; }

        [JsonProperty(PropertyName = "open_date")]
        public DateTime OpenDate { get; private set; }

        [JsonProperty(PropertyName = "leverage")]
        public double Leverage { get; private set; }

        [JsonProperty(PropertyName = "pnl")]
        public double ProfitAndLoss { get; private set; }

        [JsonProperty(PropertyName = "sfd")]
        public double SwapForDifference { get; private set; }

        public override int GetHashCode()
        {
            return ProductCode.GetHashCode() ^ Side.GetHashCode() ^ OpenDate.GetHashCode() ^ Price.GetHashCode() ^ Size.GetHashCode();
        }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfPosition[]> GetPositions(BfProductCode productCode)
        {
            return PrivateGet<BfPosition[]>(nameof(GetPositions), "product_code=" + productCode.ToEnumString());
        }
    }
}
