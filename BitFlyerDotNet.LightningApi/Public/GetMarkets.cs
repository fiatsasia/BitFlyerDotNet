//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public Task<BitFlyerResponse<BfMarket[]>> GetMarketsAsync()
        {
            return GetAsync<BfMarket[]>(nameof(GetMarkets));
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

        public Task<BitFlyerResponse<BfMarket[]>> GetMarketsUsaAsync()
        {
            return GetAsync<BfMarket[]>(nameof(GetMarkets) + UsaMarket);
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

        public Task<BitFlyerResponse<BfMarket[]>> GetMarketsEuAsync()
        {
            return GetAsync<BfMarket[]>(nameof(GetMarkets) + EuMarket);
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

        public Task<BitFlyerResponse<BfMarket[]>>[] GetMarketsAllAsync()
        {
            return new Task<BitFlyerResponse<BfMarket[]>>[]
            {
                GetMarketsAsync(),
                GetMarketsUsaAsync(),
                GetMarketsEuAsync(),
            };
        }
    }
}
