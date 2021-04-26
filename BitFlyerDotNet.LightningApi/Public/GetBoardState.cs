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
    public class BfBoardStateResult
    {
        [JsonProperty(PropertyName = "health")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfHealthState Health { get; private set; }

        [JsonProperty(PropertyName = "state")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfBoardState State { get; private set; }
    }

    public partial class BitFlyerClient
    {
        /// <summary>
        /// Exchange status details
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetBoardState">Online help</see>
        /// </summary>
        /// <param name="productCode"></param>
        /// <returns></returns>
        public Task<BitFlyerResponse<BfBoardStateResult>> GetBoardStateAsync(BfProductCode productCode, CancellationToken ct)
            => GetAsync<BfBoardStateResult>(nameof(GetBoardState), "product_code=" + productCode.ToEnumString(), ct);

        public BitFlyerResponse<BfBoardStateResult> GetBoardState(BfProductCode productCode)
            => GetBoardStateAsync(productCode, CancellationToken.None).Result;
    }
}
