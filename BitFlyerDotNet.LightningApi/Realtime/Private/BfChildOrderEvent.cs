//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    // Event sequences:
    // 1. Market price order
    //  Order -> Execution
    // 2. Market price order (partiall executed)
    //  Order -> Execution -> Execution
    // 3. FOK and killed immediately
    //  Order -> Expire
    /// <summary>
    /// Send parent order <see href="https://scrapbox.io/BitFlyerDotNet/ChildOrderEvent"/>
    /// </summary>
    public class BfChildOrderEvent
    {
        [JsonProperty(PropertyName = "product_code")]
        public string ProductCode { get; private set; }             // EventType = All

        [JsonProperty(PropertyName = "child_order_id")]
        public string ChildOrderId { get; private set; }            // EventType = All

        [JsonProperty(PropertyName = "child_order_acceptance_id")]
        public string ChildOrderAcceptanceId { get; private set; }  // EventType = All

        [JsonProperty(PropertyName = "event_date")]
        public DateTime EventDate { get; private set; }             // EventType = All

        [JsonProperty(PropertyName = "event_type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderEventType EventType { get; private set; }     // EventType = All

        [JsonProperty(PropertyName = "child_order_type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderType ChildOrderType { get; private set; }     // EventType = Order

        [JsonProperty(PropertyName = "reason")]
        public string OrderFailedReason { get; private set; }       // EventType = OrderFailed

        [JsonProperty(PropertyName = "exec_id")]
        public int ExecutionId { get; private set; }                // EventType = Execution

        [JsonProperty(PropertyName = "side")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTradeSide Side { get; private set; }               // EventType = Order, Execution

        [JsonProperty(PropertyName = "price")]
        public decimal Price { get; private set; }                  // EventType = Order(Order price), Execution(Executed price)

        [JsonProperty(PropertyName = "size")]
        public decimal Size { get; private set; }                   // EventType = Order(Order size), Execution(Executed size)

        [JsonProperty(PropertyName = "expire_date")]
        public DateTime ExpireDate { get; private set; }            // EventType = Order

        [JsonProperty(PropertyName = "commission")]
        public decimal Commission { get; private set; }             // EventType = Execution

        [JsonProperty(PropertyName = "sfd")]
        public decimal SwapForDifference { get; private set; }      // EventType = Execution

        [JsonProperty(PropertyName = "outstanding_size")]
        public decimal OutstandingSize { get; private set; }        // EventType = Execution
    }
}
