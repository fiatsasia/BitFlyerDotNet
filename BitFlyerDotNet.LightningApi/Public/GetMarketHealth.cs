//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfMarketHealth
    {
        [JsonProperty(PropertyName = "status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfHealthState Status { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfMarketHealth> GetMarketHealth(BfProductCode productCode)
        {
            return Get<BfMarketHealth>("gethealth", "product_code=" + productCode.ToEnumString());
        }
    }
}
