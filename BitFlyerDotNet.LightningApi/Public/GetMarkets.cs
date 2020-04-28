//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
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

        public BitFlyerResponse<BfMarket[]> GetMarketsUsa()
        {
            return Get<BfMarket[]>(nameof(GetMarkets) + _usaMarket);
        }

        public BitFlyerResponse<BfMarket[]> GetMarketsEu()
        {
            return Get<BfMarket[]>(nameof(GetMarkets) + _euMarket);
        }

        public BitFlyerResponse<BfMarket[]>[] GetMarketsAll()
        {
            return new BitFlyerResponse<BfMarket[]>[]
            {
                GetMarkets(),
                GetMarketsUsa(),
                GetMarketsEu()
            };
        }
    }
}
