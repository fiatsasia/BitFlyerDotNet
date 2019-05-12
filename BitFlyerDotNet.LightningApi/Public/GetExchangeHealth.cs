//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Fiats.Utils;

namespace BitFlyerDotNet.LightningApi
{
    public class BfExchangeHealth
    {
        [JsonProperty(PropertyName = "status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfBoardHealth Status { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfExchangeHealth> GetExchangeHealth(BfProductCode productCode)
        {
            return Get<BfExchangeHealth>("gethealth", "product_code=" + productCode.ToEnumString());
        }
    }
}
