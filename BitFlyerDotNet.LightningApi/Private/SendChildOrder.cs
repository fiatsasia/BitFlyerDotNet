//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfChildOrderRequest
    {
        public string ProductCode { get; set; }

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
        public static BfChildOrderRequest Market(string productCode, BfTradeSide side, decimal size, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            return new ()
            {
                ProductCode = productCode,
                ChildOrderType = BfOrderType.Market,
                Side = side,
                Size = size,
                MinuteToExpire = minuteToExpire,
                TimeInForce = timeInForce,
            };
        }

        public static BfChildOrderRequest Limit(string productCode, BfTradeSide side, decimal price, decimal size, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            return new ()
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
            if (!request.ChildOrderType.IsChildOrderType())
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
            return PostPrivateAsync<BfChildOrderResponse>(nameof(SendChildOrder), request, CancellationToken.None).Result;
        }

        /// <summary>
        /// Send a New Order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/SendChildOrder">Online help</see>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<BitFlyerResponse<BfChildOrderResponse>> SendChildOrderAsync(BfChildOrderRequest request, CancellationToken ct)
        {
            Validate(ref request);
            return PostPrivateAsync<BfChildOrderResponse>(nameof(SendChildOrder), request, ct);
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
            string productCode,
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
        public Task<BitFlyerResponse<BfChildOrderResponse>> SendChildOrderAsync(
            string productCode,
            BfOrderType orderType,
            BfTradeSide side,
            decimal price,
            decimal size,
            int minuteToExpire,
            BfTimeInForce timeInForce,
            CancellationToken ct
        )
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
            return SendChildOrderAsync(request, ct);
        }
    }
}
