//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
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
    public class BfMarketHealth
    {
        [JsonProperty(PropertyName = "status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfHealthState Status { get; private set; }
    }

    public partial class BitFlyerClient
    {
        /// <summary>
        /// Exchange status
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetMarketHealth">Online help</see>
        /// </summary>
        /// <param name="productCode"></param>
        /// <returns></returns>
        public Task<BitFlyerResponse<BfMarketHealth>> GetHealthAsync(BfProductCode productCode, CancellationToken ct)
            => GetAsync<BfMarketHealth>(nameof(GetHealth), "product_code=" + productCode.ToEnumString(), ct);

        public BitFlyerResponse<BfMarketHealth> GetHealth(BfProductCode productCode) => GetHealthAsync(productCode, CancellationToken.None).Result;
    }
}
