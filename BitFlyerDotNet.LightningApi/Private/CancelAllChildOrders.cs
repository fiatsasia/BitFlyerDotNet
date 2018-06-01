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

namespace BitFlyerDotNet.LightningApi
{
    class BfCancelAllChildOrdersRequest
    {
        [JsonProperty(PropertyName = "product_code")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfProductCode ProductCode { get; internal set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<string> CancelAllChildOrders(BfProductCode productCode)
        {
            var cancel = new BfCancelAllChildOrdersRequest
            {
                ProductCode = productCode,
            };
            return PrivatePost<string>(nameof(CancelAllChildOrders), JsonConvert.SerializeObject(cancel, _jsonSettings));
        }
    }
}
