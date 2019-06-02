//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
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
        public BitFlyerResponse<BfBoardStateResult> GetBoardState(BfProductCode productCode)
        {
            return Get<BfBoardStateResult>(nameof(GetBoardState), "product_code=" + productCode.ToEnumString());
        }
    }
}
