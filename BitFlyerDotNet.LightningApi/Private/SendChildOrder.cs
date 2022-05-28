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
    public class BfChildOrder
    {
        public string ProductCode { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderType ChildOrderType { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public BfTradeSide Side { get; set; }

        [JsonConverter(typeof(DecimalJsonConverter))]
        public decimal? Price { get; set; }
        public bool ShouldSerializePrice() => Price.HasValue;

        [JsonConverter(typeof(DecimalJsonConverter))]
        public decimal Size { get; set; }

        public int? MinuteToExpire { get; set; }
        public bool ShouldSerializeMinuteToExpire() => (MinuteToExpire.HasValue && MinuteToExpire.Value > 0); // default = 43200 (30 days)

        [JsonConverter(typeof(StringEnumConverter))]
        public BfTimeInForce? TimeInForce { get; set; }
        public bool ShouldSerializeTimeInForce() => (TimeInForce.HasValue && TimeInForce.Value != BfTimeInForce.NotSpecified); // default = GTC

        // This will be used order factory
        public static implicit operator BfParentOrderParameter(BfChildOrder order)
        {
            return new()
            {
                ProductCode = order.ProductCode,
                ConditionType = order.ChildOrderType,
                Side = order.Side,
                Price = order.Price,
                Size = order.Size
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
        void Validate(ref BfChildOrder request)
        {
            if (!request.ChildOrderType.IsChildOrderType())
            {
                throw new ArgumentException($"Invalid {nameof(BfChildOrder.ChildOrderType)} is {request.ChildOrderType}");
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
        /// <param name="order"></param>
        /// <returns></returns>
        public Task<BitFlyerResponse<BfChildOrderResponse>> SendChildOrderAsync(BfChildOrder order, CancellationToken ct)
        {
            Validate(ref order);
            return PostPrivateAsync<BfChildOrderResponse>(nameof(SendChildOrderAsync), order, ct);
        }

        /// <summary>
        /// Send a New Order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/SendChildOrder">Online help</see>
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public async Task<BfChildOrderResponse> SendChildOrderAsync(BfChildOrder order)
        {
            return (await SendChildOrderAsync(order, CancellationToken.None)).GetContent();
        }
    }
}
