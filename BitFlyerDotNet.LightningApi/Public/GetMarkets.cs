//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using Newtonsoft.Json;

namespace BitFlyerDotNet.LightningApi
{
    public class BfMarket
    {
        [JsonProperty(PropertyName = "product_code")]
        public string ProductCode { get; private set; }

        [JsonProperty(PropertyName = "alias")]
        public string Alias { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfMarket[]> GetMarkets()
        {
            return Get<BfMarket[]>(nameof(GetMarkets));
        }
    }
}
