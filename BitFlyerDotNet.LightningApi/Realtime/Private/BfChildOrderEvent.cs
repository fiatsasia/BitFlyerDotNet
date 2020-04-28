//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfChildOrderEvent
    {
        [JsonProperty(PropertyName = "product_code")]
        public string ProductCode { get; private set; }

        [JsonProperty(PropertyName = "child_order_id")]
        public string ChildOrderId { get; private set; }

        [JsonProperty(PropertyName = "child_order_acceptance_id")]
        public string ChildOrderAcceptanceId { get; private set; }

        [JsonProperty(PropertyName = "event_date")]
        public DateTime EventDate { get; private set; }

        [JsonProperty(PropertyName = "event_type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderEventType EventType { get; private set; }

        [JsonProperty(PropertyName = "child_order_type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderType ChildOrderType { get; private set; }     // EventType = Order

        [JsonProperty(PropertyName = "expire_date")]
        public DateTime ExpireDate { get; private set; }            // EventType = Order, Execution

        [JsonProperty(PropertyName = "reason")]
        public string OrderFailedReason { get; private set; }       // EventType = OrderFailed

        [JsonProperty(PropertyName = "exec_id")]
        public int ExecutionId { get; private set; }                // EventType = Execution

        [JsonProperty(PropertyName = "side")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTradeSide Side { get; private set; }               // EventType = Order, Execution

        [JsonProperty(PropertyName = "price")]
        public decimal Price { get; private set; }                  // EventType = Order, Execution

        [JsonProperty(PropertyName = "size")]
        public decimal Size { get; private set; }                   // EventType = Order, Execution

        [JsonProperty(PropertyName = "commission")]
        public decimal Commission { get; private set; }             // EventType = Execution

        [JsonProperty(PropertyName = "sfd")]
        public decimal SfdCollectedAmount { get; private set; }     // EventType = Execution
    }
}
