//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfCancelChildOrderRequest
    {
        [JsonProperty(PropertyName = "product_code")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfProductCode ProductCode { get; set; }

        [JsonProperty(PropertyName = "child_order_id")]
        public string ChildOrderId { get; set; }
        public bool ShouldSerializeChildOrderId() { return !string.IsNullOrEmpty(ChildOrderId); }

        [JsonProperty(PropertyName = "child_order_acceptance_id")]
        public string ChildOrderAcceptanceId { get; set; }
        public bool ShouldSerializeChildOrderAcceptanceId() { return !string.IsNullOrEmpty(ChildOrderAcceptanceId); }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<string> CancelChildOrder(BfCancelChildOrderRequest request)
        {
            return PrivatePost<string>(nameof(CancelChildOrder), JsonConvert.SerializeObject(request, _jsonSettings));
        }

        public BitFlyerResponse<string> CancelChildOrder(BfProductCode productCode, string childOrderId = null, string childOrderAcceptanceId = null)
        {
            if (string.IsNullOrEmpty(childOrderId) && string.IsNullOrEmpty(childOrderAcceptanceId))
            {
                throw new ArgumentException();
            }

            return CancelChildOrder(new BfCancelChildOrderRequest
            {
                ProductCode = productCode,
                ChildOrderId = childOrderId,
                ChildOrderAcceptanceId = childOrderAcceptanceId
            });
        }
    }
}
