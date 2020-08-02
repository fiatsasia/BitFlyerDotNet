//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

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
        public BitFlyerResponse<BfBoardStateResult> GetBoardState(BfProductCode productCode)
        {
            return GetAsync<BfBoardStateResult>(nameof(GetBoardState), "product_code=" + productCode.ToEnumString()).Result;
        }
    }
}
