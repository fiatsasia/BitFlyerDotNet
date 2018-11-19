//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfCancelParentOrderRequest
    {
        [JsonProperty(PropertyName = "product_code")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfProductCode ProductCode { get; set; }

        [JsonProperty(PropertyName = "parent_order_id")]
        public string ParentOrderId { get; set; }
        public bool ShouldSerializeParentOrderId() { return !string.IsNullOrEmpty(ParentOrderId); }

        [JsonProperty(PropertyName = "parent_order_acceptance_id")]
        public string ParentOrderAcceptanceId { get; set; }
        public bool ShouldSerializeParentOrderAcceptanceId() { return !string.IsNullOrEmpty(ParentOrderAcceptanceId); }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<string> CancelParentOrder(BfCancelParentOrderRequest request)
        {
            return PrivatePost<string>(nameof(CancelParentOrder), JsonConvert.SerializeObject(request, _jsonSettings));
        }

        public BitFlyerResponse<string> CancelParentOrder(BfProductCode productCode, string parentOrderId = null, string parentOrderAcceptanceId = null)
        {
            if (string.IsNullOrEmpty(parentOrderId) && string.IsNullOrEmpty(parentOrderAcceptanceId))
            {
                throw new ArgumentException();
            }

            return CancelParentOrder(new BfCancelParentOrderRequest
            {
                ProductCode = productCode,
                ParentOrderId = parentOrderId,
                ParentOrderAcceptanceId = parentOrderAcceptanceId
            });
        }
    }
}
