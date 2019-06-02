//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public interface IBfChildOrderRequest
    {
        BfProductCode ProductCode { get; }
        BfOrderType OrderType { get; }
        BfTradeSide Side { get; }
        decimal Size { get; }
        decimal Price { get; }
    }

    public class BfChildOrderRequest : IBfChildOrderRequest
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
        public decimal Size { get; set; }

        [JsonProperty(PropertyName = "price")]
        [JsonConverter(typeof(DecimalJsonConverter))]
        public decimal Price { get; set; }
        public bool ShouldSerializePrice() { return OrderType == BfOrderType.Limit; }

        [JsonProperty(PropertyName = "minute_to_expire")]
        public int MinuteToExpire { get; set; } = BitFlyerClientConfig.MinuteToExpireDefault;
        public bool ShouldSerializeMinuteToExpire()
            { return MinuteToExpire != BitFlyerClientConfig.MinuteToExpireDefault && MinuteToExpire > 0; } // default = 43200 (30 days)

        [JsonProperty(PropertyName = "time_in_force")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTimeInForce TimeInForce { get; set; } = BitFlyerClientConfig.TimeInForceDefault;
        public bool ShouldSerializeTimeInForce()
            { return TimeInForce != BitFlyerClientConfig.TimeInForceDefault && TimeInForce != BfTimeInForce.NotSpecified; } // default = GTC

        public TimeSpan MinuteToExpireSpan { get => TimeSpan.FromMinutes(MinuteToExpire); set => MinuteToExpire = (int)value.TotalMinutes; }
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
            if (request.MinuteToExpire == BitFlyerClientConfig.MinuteToExpireDefault && Config.MinuteToExpire != BitFlyerClientConfig.MinuteToExpireDefault)
            {
                request.MinuteToExpire = Config.MinuteToExpire;
            }

            if (request.TimeInForce == BitFlyerClientConfig.TimeInForceDefault && Config.TimeInForce != BitFlyerClientConfig.TimeInForceDefault)
            {
                request.TimeInForce = Config.TimeInForce;
            }

            var jsonRequest = JsonConvert.SerializeObject(request, _jsonSettings);
            return PrivatePost<BfChildOrderResponse>(nameof(SendChildOrder), jsonRequest);
        }

        public BitFlyerResponse<BfChildOrderResponse> SendChildOrder(
            BfProductCode productCode,
            BfOrderType orderType,
            BfTradeSide side,
            decimal price,
            decimal size,
            int minuteToExpire = BitFlyerClientConfig.MinuteToExpireDefault,
            BfTimeInForce timeInForce = BitFlyerClientConfig.TimeInForceDefault)
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
