//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfCancelAllChildOrdersRequest
    {
        [JsonProperty(PropertyName = "product_code")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfProductCode ProductCode { get; set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<string> CancelAllChildOrders(BfCancelAllChildOrdersRequest request)
        {
            return PrivatePost<string>(nameof(CancelAllChildOrders), JsonConvert.SerializeObject(request, _jsonSettings));
        }

        public BitFlyerResponse<string> CancelAllChildOrders(BfProductCode productCode)
        {
            return CancelAllChildOrders(new BfCancelAllChildOrdersRequest
            {
                ProductCode = productCode,
            });
        }
    }
}
