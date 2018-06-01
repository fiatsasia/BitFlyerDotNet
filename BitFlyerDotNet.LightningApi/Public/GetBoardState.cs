//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Fiats.Utils;

namespace BitFlyerDotNet.LightningApi
{
    public class BfBoardStateResult
    {
        [JsonProperty(PropertyName = "health")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfBoardHealth Health { get; private set; }

        [JsonProperty(PropertyName = "state")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfBoardState State { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfBoardStateResult> GetBoardState(BfProductCode productCode)
        {
            return Get<BfBoardStateResult>(nameof(GetBoardState), string.Format("product_code={0}", productCode.ToEnumString()));
        }
    }
}
