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
    class BfCancelChildOrderRequest
    {
        [JsonProperty(PropertyName = "product_code")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfProductCode ProductCode { get; internal set; }

        [JsonProperty(PropertyName = "child_order_id")]
        public string ChildOrderId { get; internal set; }
        public bool ShouldSerializeChildOrderId() { return !string.IsNullOrEmpty(ChildOrderId); }

        [JsonProperty(PropertyName = "child_order_acceptance_id")]
        public string ChildOrderAcceptanceId { get; internal set; }
        public bool ShouldSerializeChildOrderAcceptanceId() { return !string.IsNullOrEmpty(ChildOrderAcceptanceId); }
    }

    public partial class BitFlyerClient
    {
        BitFlyerResponse<string> CancelChildOrder(BfCancelChildOrderRequest cancel)
        {
            return PrivatePost<string>(nameof(CancelChildOrder), JsonConvert.SerializeObject(cancel, _jsonSettings));
        }

        public BitFlyerResponse<string> CancelChildOrderByOrderId(BfProductCode productCode, string childOrderId)
        {
            var cancel = new BfCancelChildOrderRequest
            {
                ProductCode = productCode,
                ChildOrderId = childOrderId
            };
            return CancelChildOrder(cancel);
        }

        public BitFlyerResponse<string> CancelChildOrderByAcceptanceId(BfProductCode productCode, string childOrderAcceptanceId)
        {
            var cancel = new BfCancelChildOrderRequest
            {
                ProductCode = productCode,
                ChildOrderAcceptanceId = childOrderAcceptanceId
            };
            return CancelChildOrder(cancel);
        }
    }
}
