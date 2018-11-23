//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfChildOrderRequest
    {
        [JsonProperty(PropertyName = "product_code")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfProductCode ProductCode { get; set; }

        [JsonProperty(PropertyName = "child_order_type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderType OrderType { get; set; }

        [JsonProperty(PropertyName = "side")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTradeSide Side { get; set; }

        [JsonProperty(PropertyName = "size")]
        public double Size { get; set; }

        [JsonProperty(PropertyName = "price")]
        [JsonConverter(typeof(DecimalJsonConverter))]
        public double Price { get; set; }
        public bool ShouldSerializePrice() { return OrderType == BfOrderType.Limit; }

        [JsonProperty(PropertyName = "minute_to_expire")]
        public int MinuteToExpire { get; set; }
        public bool ShouldSerializeMinuteToExpire() { return MinuteToExpire > 0; } // default = 43200 (30 days)

        [JsonProperty(PropertyName = "time_in_force")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTimeInForce TimeInForce { get; set; }
        public bool ShouldSerializeTimeInForce() { return TimeInForce != BfTimeInForce.NotSpecified; } // default = GTC
    }

    public class BfChildOrderResponse
    {
        [JsonProperty(PropertyName = "child_order_acceptance_id")]
        public string ChildOrderAcceptanceId { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfChildOrderResponse> SendChildOrder(BfChildOrderRequest request)
        {
            var jsonRequest = JsonConvert.SerializeObject(request, _jsonSettings);
            return PrivatePost<BfChildOrderResponse>(nameof(SendChildOrder), jsonRequest);
        }

        public BitFlyerResponse<BfChildOrderResponse> SendChildOrder(
            BfProductCode productCode,
            BfOrderType orderType,
            BfTradeSide side,
            double price,
            double size,
            int minuteToExpire = 0,
            BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            return SendChildOrder(new BfChildOrderRequest
            {
                ProductCode = productCode,
                OrderType = orderType,
                Side = side,
                Price = price,
                Size = size,
                MinuteToExpire = minuteToExpire,
                TimeInForce = timeInForce,
            });
        }
    }
}
