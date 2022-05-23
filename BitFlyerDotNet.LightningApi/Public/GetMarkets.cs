//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System.Threading;
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
        public Task<BitFlyerResponse<BfMarket[]>> GetMarketsAsync(CancellationToken ct) => GetAsync<BfMarket[]>(nameof(GetMarkets), string.Empty, ct);

        public BitFlyerResponse<BfMarket[]> GetMarkets() => GetMarketsAsync(CancellationToken.None).Result;

        /// <summary>
        /// Market List (U.S.)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetMarkets">Online help</see>
        /// </summary>
        /// <returns></returns>
        public Task<BitFlyerResponse<BfMarket[]>> GetMarketsUsaAsync(CancellationToken ct) => GetAsync<BfMarket[]>(nameof(GetMarkets) + UsaMarket, string.Empty, ct);

        public BitFlyerResponse<BfMarket[]> GetMarketsUsa() => GetMarketsUsaAsync(CancellationToken.None).Result;

        /// <summary>
        /// Market List (E.U.)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetMarkets">Online help</see>
        /// </summary>
        /// <returns></returns>
        public Task<BitFlyerResponse<BfMarket[]>> GetMarketsEuAsync(CancellationToken ct) => GetAsync<BfMarket[]>(nameof(GetMarkets) + EuMarket, string.Empty, ct);

        public BitFlyerResponse<BfMarket[]> GetMarketsEu() => GetMarketsEuAsync(CancellationToken.None).Result;

        /// <summary>
        /// Market List (All countries)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetMarkets">Online help</see>
        /// </summary>
        /// <returns></returns>
        public Task<BitFlyerResponse<BfMarket[]>>[] GetMarketsAllAsync(CancellationToken ct)
        {
            return new Task<BitFlyerResponse<BfMarket[]>>[] { GetMarketsAsync(ct), GetMarketsUsaAsync(ct), GetMarketsEuAsync(ct) };
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
