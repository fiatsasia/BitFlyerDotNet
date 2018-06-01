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
    public class BfExchangeHealth
    {
        [JsonProperty(PropertyName = "status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfBoardHealth Status { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfExchangeHealth> GetExchangeHealth(BfProductCode productCode)
        {
            return Get<BfExchangeHealth>("gethealth", string.Format("product_code={0}", productCode.ToEnumString()));
        }
    }
}
