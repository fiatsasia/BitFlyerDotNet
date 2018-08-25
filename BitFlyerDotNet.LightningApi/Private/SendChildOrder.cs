//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    class BfChildOrderRequest
    {
        [JsonProperty(PropertyName = "product_code")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfProductCode ProductCode { get; internal set; }

        [JsonProperty(PropertyName = "child_order_type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderType OrderType { get; internal set; }

        [JsonProperty(PropertyName = "side")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTradeSide Side { get; internal set; }

        [JsonProperty(PropertyName = "price")]
        [JsonConverter(typeof(DecimalJsonConverter))]
        public double Price { get; internal set; }
        public bool ShouldSerializePrice() { return OrderType == BfOrderType.Limit; }

        [JsonProperty(PropertyName = "size")]
        public double Size { get; internal set; }

        [JsonProperty(PropertyName = "minute_to_expire")]
        public int MinuteToExpire { get; internal set; }
        public bool ShouldSerializeMinuteToExpire() { return MinuteToExpire > 0; } // default = 43200 (30 days)

        [JsonProperty(PropertyName = "time_in_force")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTimeInForce TimeInForce { get; internal set; }
        public bool ShouldSerializeTimeInForce() { return TimeInForce != BfTimeInForce.NotSpecified; } // default = GTC
    }

    public class ChildOrderResponse
    {
        [JsonProperty(PropertyName = "child_order_acceptance_id")]
        public string ChildOrderAcceptanceId { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<ChildOrderResponse> SendChildOrder(
            BfProductCode productCode,
            BfOrderType orderType,
            BfTradeSide side,
            double price,
            double size,
            int minuteToExpire = 0,
            BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            var order = new BfChildOrderRequest
            {
                ProductCode = productCode,
                OrderType = orderType,
                Side = side,
                Price = price,
                Size = size,
                MinuteToExpire = minuteToExpire,
                TimeInForce = timeInForce,
            };
            var jsonRequest = JsonConvert.SerializeObject(order, _jsonSettings);
            return PrivatePost<ChildOrderResponse>(nameof(SendChildOrder), jsonRequest);
        }
    }
}
