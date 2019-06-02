//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfParentOrderRequestParameter : IBfChildOrderRequest
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
        public decimal Size { get; set; }

        [JsonProperty(PropertyName = "price")]
        [JsonConverter(typeof(DecimalJsonConverter))]
        public decimal Price { get; set; }
        public bool ShouldSerializePrice() { return ConditionType == BfOrderType.Limit || ConditionType == BfOrderType.StopLimit; }

        [JsonProperty(PropertyName = "trigger_price")]
        public decimal TriggerPrice { get; set; }
        public bool ShouldSerializeTriggerPrice() { return ConditionType == BfOrderType.Stop || ConditionType == BfOrderType.StopLimit; }

        [JsonProperty(PropertyName = "offset")]
        [JsonConverter(typeof(DecimalJsonConverter))]
        public decimal Offset { get; set; }
        public bool ShouldSerializeOffset() { return ConditionType == BfOrderType.Trail; }

        // BfChildOrderRequest compatibility
        public BfOrderType OrderType => ConditionType;
    }

    public class BfParentOrderRequest
    {
        [JsonProperty(PropertyName = "order_method")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderType OrderMethod { get; set; }

        [JsonProperty(PropertyName = "minute_to_expire")]
        public int MinuteToExpire { get; set; } = BitFlyerClientConfig.MinuteToExpireDefault;
        public bool ShouldSerializeMinuteToExpire()
            { return MinuteToExpire != BitFlyerClientConfig.MinuteToExpireDefault && MinuteToExpire > 0; } // default = 43200 (30 days)

        [JsonProperty(PropertyName = "time_in_force")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTimeInForce TimeInForce { get; set; } = BitFlyerClientConfig.TimeInForceDefault;
        public bool ShouldSerializeTimeInForce()
            { return TimeInForce != BitFlyerClientConfig.TimeInForceDefault && TimeInForce != BfTimeInForce.NotSpecified; } // default = GTC

        [JsonProperty(PropertyName = "parameters")]
        public List<BfParentOrderRequestParameter> Paremters { get; } = new List<BfParentOrderRequestParameter>();
    }

    public class BfParentOrderResponse
    {
        [JsonProperty(PropertyName = "parent_order_acceptance_id")]
        public string ParentOrderAcceptanceId { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfParentOrderResponse> SendParentOrder(BfParentOrderRequest request)
        {
            if (request.MinuteToExpire == BitFlyerClientConfig.MinuteToExpireDefault && Config.MinuteToExpire != BitFlyerClientConfig.MinuteToExpireDefault)
            {
                request.MinuteToExpire = Config.MinuteToExpire;
            }

            if (request.TimeInForce == BitFlyerClientConfig.TimeInForceDefault && Config.TimeInForce != BitFlyerClientConfig.TimeInForceDefault)
            {
                request.TimeInForce = Config.TimeInForce;
            }

            var jsonRequest = JsonConvert.SerializeObject(request, _jsonSettings);
            return PrivatePost<BfParentOrderResponse>(nameof(SendParentOrder), jsonRequest);
        }

        public BitFlyerResponse<BfParentOrderResponse> SendParentOrder(BfParentOrderRequest request, params BfParentOrderRequestParameter[] childOrders)
        {
            if (request.MinuteToExpire == BitFlyerClientConfig.MinuteToExpireDefault && Config.MinuteToExpire != BitFlyerClientConfig.MinuteToExpireDefault)
            {
                request.MinuteToExpire = Config.MinuteToExpire;
            }

            if (request.TimeInForce == BitFlyerClientConfig.TimeInForceDefault && Config.TimeInForce != BitFlyerClientConfig.TimeInForceDefault)
            {
                request.TimeInForce = Config.TimeInForce;
            }

            request.Paremters.AddRange(childOrders);
            var jsonRequest = JsonConvert.SerializeObject(request, _jsonSettings);
            return PrivatePost<BfParentOrderResponse>(nameof(SendParentOrder), jsonRequest);
        }
    }
}
