//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfParentOrderRequestParameter
    {
        [JsonProperty(PropertyName = "product_code")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfProductCode ProductCode { get; set; }

        [JsonProperty(PropertyName = "condition_type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderType ConditionType { get; set; }

        [JsonProperty(PropertyName = "side")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTradeSide Side { get; set; }

        [JsonProperty(PropertyName = "size")]
        public double Size { get; set; }

        [JsonProperty(PropertyName = "price")]
        [JsonConverter(typeof(DecimalJsonConverter))]
        public double Price { get; set; }
        public bool ShouldSerializePrice() { return ConditionType == BfOrderType.Limit || ConditionType == BfOrderType.StopLimit; }

        [JsonProperty(PropertyName = "trigger_price")]
        public double TriggerPrice { get; set; }
        public bool ShouldSerializeTriggerPrice() { return ConditionType == BfOrderType.Stop || ConditionType == BfOrderType.StopLimit; }

        [JsonProperty(PropertyName = "offset")]
        public int Offset { get; set; }
        public bool ShouldSerializeOffset() { return ConditionType == BfOrderType.Trail; }
    }

    public class BfParentOrderRequest
    {
        [JsonProperty(PropertyName = "order_method")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderMethod OrderMethod { get; set; }

        [JsonProperty(PropertyName = "minute_to_expire")]
        public int MinuteToExpire { get; set; }
        public bool ShouldSerializeMinuteToExpire() { return MinuteToExpire > 0; } // default = 43200 (30 days)

        [JsonProperty(PropertyName = "time_in_force")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTimeInForce TimeInForce { get; set; }
        public bool ShouldSerializeTimeInForce() { return TimeInForce != BfTimeInForce.NotSpecified; } // default = GTC

        [JsonProperty(PropertyName = "parameters")]
        internal List<BfParentOrderRequestParameter> Paremters { get; } = new List<BfParentOrderRequestParameter>();

        public void AddChildOrder(BfParentOrderRequestParameter order)
        {
            Paremters.Add(order);
        }
    }

    public class ParentOrderResponse
    {
        [JsonProperty(PropertyName = "parent_order_acceptance_id")]
        public string ParentOrderAcceptanceId { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<ParentOrderResponse> SendParentOrder(BfParentOrderRequest order)
        {
            var jsonRequest = JsonConvert.SerializeObject(order, _jsonSettings);
            return PrivatePost<ParentOrderResponse>(nameof(SendParentOrder), jsonRequest);
        }
    }
}
