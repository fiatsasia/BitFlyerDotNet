//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    class BfCancelParentOrderRequest
    {
        [JsonProperty(PropertyName = "product_code")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfProductCode ProductCode { get; internal set; }

        [JsonProperty(PropertyName = "parent_order_id")]
        public string ParentOrderId { get; internal set; }
        public bool ShouldSerializeParentOrderId() { return !string.IsNullOrEmpty(ParentOrderId); }

        [JsonProperty(PropertyName = "parent_order_acceptance_id")]
        public string ParentOrderAcceptanceId { get; internal set; }
        public bool ShouldSerializeParentOrderAcceptanceId() { return !string.IsNullOrEmpty(ParentOrderAcceptanceId); }
    }

    public partial class BitFlyerClient
    {
        BitFlyerResponse<string> CancelParentOrder(BfCancelParentOrderRequest cancel)
        {
            return PrivatePost<string>(nameof(CancelParentOrder), JsonConvert.SerializeObject(cancel, _jsonSettings));
        }

        public BitFlyerResponse<string> CancelParentOrderByOrderId(BfProductCode productCode, string parentOrderId)
        {
            var cancel = new BfCancelParentOrderRequest
            {
                ProductCode = productCode,
                ParentOrderId = parentOrderId
            };
            return CancelParentOrder(cancel);
        }

        public BitFlyerResponse<string> CancelParentOrderByAcceptanceId(BfProductCode productCode, string parentOrderAcceptanceId)
        {
            var cancel = new BfCancelParentOrderRequest
            {
                ProductCode = productCode,
                ParentOrderAcceptanceId = parentOrderAcceptanceId
            };
            return CancelParentOrder(cancel);
        }
    }
}
