//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfParentOrderEvent
    {
        [JsonProperty(PropertyName = "product_code")]
        public string ProductCode { get; private set; }

        [JsonProperty(PropertyName = "parent_order_id")]
        public string ParentOrderId { get; private set; }

        [JsonProperty(PropertyName = "parent_order_acceptance_id")]
        public string ParentOrderAcceptanceId { get; private set; }

        [JsonProperty(PropertyName = "event_date")]
        public DateTime EventDate { get; private set; }

        [JsonProperty(PropertyName = "event_type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderEventType EventType { get; private set; }

        [JsonProperty(PropertyName = "parent_order_type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderType ParentOrderType { get; private set; }    // EventType = Order

        [JsonProperty(PropertyName = "reason")]
        public string OrderFailedReason { get; private set; }       // EventType = OrderFailed

        [JsonProperty(PropertyName = "child_order_type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderType ChildOrderType { get; private set; }     // EventType = Trigger

        [JsonProperty(PropertyName = "parameter_index")]
        public int ChildOrderIndex { get; private set; }            // EventType = Trigger, Complete

        [JsonProperty(PropertyName = "child_order_acceptance_id")]
        public string ChildOrderAcceptanceId { get; private set; }  // EventType = Trigger, Complete

        [JsonProperty(PropertyName = "side")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTradeSide Side { get; private set; }               // EventType = Trigger

        [JsonProperty(PropertyName = "price")]
        public decimal Price { get; private set; }                  // EventType = Trigger

        [JsonProperty(PropertyName = "size")]
        public decimal Size { get; private set; }                   // EventType = Trigger

        [JsonProperty(PropertyName = "expire_date")]
        public DateTime ExpireDate { get; private set; }            // EventType = Order, Trigger
    }
}
