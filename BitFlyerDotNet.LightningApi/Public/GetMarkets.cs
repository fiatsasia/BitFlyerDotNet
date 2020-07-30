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
            return Get<BfMarket[]>(nameof(GetMarkets));
        }

        /// <summary>
        /// Market List (U.S.)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetMarkets">Online help</see>
        /// </summary>
        /// <returns></returns>
        public BitFlyerResponse<BfMarket[]> GetMarketsUsa()
        {
            return Get<BfMarket[]>(nameof(GetMarkets) + UsaMarket);
        }

        /// <summary>
        /// Market List (E.U.)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetMarkets">Online help</see>
        /// </summary>
        /// <returns></returns>
        public BitFlyerResponse<BfMarket[]> GetMarketsEu()
        {
            return Get<BfMarket[]>(nameof(GetMarkets) + EuMarket);
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
