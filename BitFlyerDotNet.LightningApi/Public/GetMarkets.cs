//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfMarket
    {
        [JsonProperty(PropertyName = "product_code")]
        public string ProductCode { get; private set; }

        [JsonProperty(PropertyName = "market_type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfMarketType MarketType { get; private set; }

        [JsonProperty(PropertyName = "alias")]
        public string Alias { get; private set; }
    }

    public partial class BitFlyerClient
    {
        /// <summary>
        /// Market List (Japan)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetMarkets">Online help</see>
        /// </summary>
        /// <returns></returns>
        public BitFlyerResponse<BfMarket[]> GetMarkets()
        {
            return GetAsync<BfMarket[]>(nameof(GetMarkets)).Result;
        }

        /// <summary>
        /// Market List (U.S.)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetMarkets">Online help</see>
        /// </summary>
        /// <returns></returns>
        public BitFlyerResponse<BfMarket[]> GetMarketsUsa()
        {
            return GetAsync<BfMarket[]>(nameof(GetMarkets) + UsaMarket).Result;
        }

        /// <summary>
        /// Market List (E.U.)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetMarkets">Online help</see>
        /// </summary>
        /// <returns></returns>
        public BitFlyerResponse<BfMarket[]> GetMarketsEu()
        {
            return GetAsync<BfMarket[]>(nameof(GetMarkets) + EuMarket).Result;
        }

        /// <summary>
        /// Market List (All countries)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetMarkets">Online help</see>
        /// </summary>
        /// <returns></returns>
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
