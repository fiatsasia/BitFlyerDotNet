//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfChildOrderRequest
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public BfProductCode ProductCode { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderType ChildOrderType { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public BfTradeSide Side { get; set; }

        [JsonConverter(typeof(DecimalJsonConverter))]
        public decimal Price { get; set; }
        public bool ShouldSerializePrice() { return ChildOrderType == BfOrderType.Limit; }

        [JsonConverter(typeof(DecimalJsonConverter))]
        public decimal Size { get; set; }

        public int MinuteToExpire { get; set; }
        public bool ShouldSerializeMinuteToExpire() { return MinuteToExpire > 0; } // default = 43200 (30 days)

        [JsonConverter(typeof(StringEnumConverter))]
        public BfTimeInForce TimeInForce { get; set; }
        public bool ShouldSerializeTimeInForce() { return TimeInForce != BfTimeInForce.NotSpecified; } // default = GTC

        // Order builders
        public static BfChildOrderRequest MarketPrice(BfProductCode productCode, BfTradeSide side, decimal size, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            return new BfChildOrderRequest
            {
                ProductCode = productCode,
                ChildOrderType = BfOrderType.Market,
                Side = side,
                Size = size,
                MinuteToExpire = minuteToExpire,
                TimeInForce = timeInForce,
            };
        }

        public static BfChildOrderRequest LimitPrice(BfProductCode productCode, BfTradeSide side, decimal price, decimal size, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            return new BfChildOrderRequest
            {
                ProductCode = productCode,
                ChildOrderType = BfOrderType.Limit,
                Side = side,
                Size = size,
                Price = price,
                MinuteToExpire = minuteToExpire,
                TimeInForce = timeInForce,
            };
        }
    }

    public class BfChildOrderResponse
    {
        [JsonProperty(PropertyName = "child_order_acceptance_id")]
        public string ChildOrderAcceptanceId { get; private set; }
    }

    public partial class BitFlyerClient
    {
        void Validate(ref BfChildOrderRequest request)
        {
            if (!request.ChildOrderType.IsSimple())
            {
                throw new ArgumentException($"Invalid {nameof(BfChildOrderRequest.ChildOrderType)} is {request.ChildOrderType}");
            }

            if (request.MinuteToExpire == 0 && Config.MinuteToExpire != 0)
            {
                request.MinuteToExpire = Config.MinuteToExpire;
            }

            if (request.TimeInForce == BfTimeInForce.NotSpecified && Config.TimeInForce != BfTimeInForce.NotSpecified)
            {
                request.TimeInForce = Config.TimeInForce;
            }
        }

        /// <summary>
        /// Send a New Order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/SendChildOrder">Online help</see>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public BitFlyerResponse<BfChildOrderResponse> SendChildOrder(BfChildOrderRequest request)
        {
            Validate(ref request);
            return PrivatePostAsync<BfChildOrderResponse>(nameof(SendChildOrder), request).Result;
        }

        /// <summary>
        /// Send a New Order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/SendChildOrder">Online help</see>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BitFlyerResponse<BfChildOrderResponse>> SendChildOrderAsync(BfChildOrderRequest request)
        {
            Validate(ref request);
            return await PrivatePostAsync<BfChildOrderResponse>(nameof(SendChildOrder), request);
        }

        /// <summary>
        /// Send a New Order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/SendChildOrder">Online help</see>
        /// </summary>
        /// <param name="productCode"></param>
        /// <param name="orderType"></param>
        /// <param name="side"></param>
        /// <param name="price"></param>
        /// <param name="size"></param>
        /// <param name="minuteToExpire"></param>
        /// <param name="timeInForce"></param>
        /// <returns></returns>
        public BitFlyerResponse<BfChildOrderResponse> SendChildOrder(
            BfProductCode productCode,
            BfOrderType orderType,
            BfTradeSide side,
            decimal price,
            decimal size,
            int minuteToExpire = 0,
            BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            var request = new BfChildOrderRequest
            {
                ProductCode = productCode,
                ChildOrderType = orderType,
                Side = side,
                Price = price,
                Size = size,
                MinuteToExpire = minuteToExpire,
                TimeInForce = timeInForce,
            };
            Validate(ref request);
            return SendChildOrder(request);
        }

        /// <summary>
        /// Send a New Order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/SendChildOrder">Online help</see>
        /// </summary>
        /// <param name="productCode"></param>
        /// <param name="orderType"></param>
        /// <param name="side"></param>
        /// <param name="price"></param>
        /// <param name="size"></param>
        /// <param name="minuteToExpire"></param>
        /// <param name="timeInForce"></param>
        /// <returns></returns>
        public async Task<BitFlyerResponse<BfChildOrderResponse>> SendChildOrderAsync(
            BfProductCode productCode,
            BfOrderType orderType,
            BfTradeSide side,
            decimal price,
            decimal size,
            int minuteToExpire = 0,
            BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            var request = new BfChildOrderRequest
            {
                ProductCode = productCode,
                ChildOrderType = orderType,
                Side = side,
                Price = price,
                Size = size,
                MinuteToExpire = minuteToExpire,
                TimeInForce = timeInForce,
            };
            Validate(ref request);
            return await SendChildOrderAsync(request);
        }
    }
}
