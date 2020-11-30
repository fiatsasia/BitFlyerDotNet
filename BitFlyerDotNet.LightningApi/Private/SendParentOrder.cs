//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfParentOrderRequestParameter
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public BfProductCode ProductCode { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderType ConditionType { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public BfTradeSide Side { get; set; }

        [JsonConverter(typeof(DecimalJsonConverter))]
        public decimal Price { get; set; }
        public bool ShouldSerializePrice() { return ConditionType == BfOrderType.Limit || ConditionType == BfOrderType.StopLimit; }

        [JsonConverter(typeof(DecimalJsonConverter))]
        public decimal Size { get; set; }

        [JsonConverter(typeof(DecimalJsonConverter))]
        public decimal TriggerPrice { get; set; }
        public bool ShouldSerializeTriggerPrice() { return ConditionType == BfOrderType.Stop || ConditionType == BfOrderType.StopLimit; }

        [JsonConverter(typeof(DecimalJsonConverter))]
        public decimal Offset { get; set; }
        public bool ShouldSerializeOffset() { return ConditionType == BfOrderType.Trail; }

        // Message builders
        public static BfParentOrderRequestParameter Market(BfProductCode productCode, BfTradeSide side, decimal size)
        {
            return new ()
            {
                ProductCode = productCode,
                ConditionType = BfOrderType.Market,
                Side = side,
                Size = size,
            };
        }

        public static BfParentOrderRequestParameter Limit(BfProductCode productCode, BfTradeSide side, decimal price, decimal size)
        {
            return new ()
            {
                ProductCode = productCode,
                ConditionType = BfOrderType.Limit,
                Side = side,
                Size = size,
                Price = price,
            };
        }

        public static BfParentOrderRequestParameter Stop(BfProductCode productCode, BfTradeSide side, decimal triggerPrice, decimal size)
        {
            if (size > triggerPrice)
            {
                throw new ArgumentException();
            }

            return new ()
            {
                ProductCode = productCode,
                ConditionType = BfOrderType.Stop,
                Side = side,
                TriggerPrice = triggerPrice,
                Size = size,
            };
        }

        public static BfParentOrderRequestParameter StopLimit(BfProductCode productCode, BfTradeSide side, decimal triggerPrice, decimal price, decimal size)
        {
            if (size > price || size > triggerPrice)
            {
                throw new ArgumentException();
            }

            return new ()
            {
                ProductCode = productCode,
                ConditionType = BfOrderType.StopLimit,
                Side = side,
                Price = price,
                Size = size,
                TriggerPrice = triggerPrice
            };
        }

        public static BfParentOrderRequestParameter Trail(BfProductCode productCode, BfTradeSide side, decimal offset, decimal size)
        {
            if (offset <= 0m)
            {
                throw new ArgumentException();
            }

            return new ()
            {
                ProductCode = productCode,
                ConditionType = BfOrderType.Trail,
                Side = side,
                Offset = offset,
                Size = size,
            };
        }
    }

    public class BfParentOrderRequest
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderType OrderMethod { get; set; }

        public int MinuteToExpire { get; set; }
        public bool ShouldSerializeMinuteToExpire() { return MinuteToExpire > 0; } // default = 43200 (30 days)

        [JsonConverter(typeof(StringEnumConverter))]
        public BfTimeInForce TimeInForce { get; set; }
        public bool ShouldSerializeTimeInForce() { return TimeInForce != BfTimeInForce.NotSpecified; } // default = GTC

        public List<BfParentOrderRequestParameter> Parameters { get; set; } = new ();

        // Message builders
        public static BfParentOrderRequest Stop(BfProductCode productCode, BfTradeSide side, decimal triggerPrice, decimal size, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            return new ()
            {
                OrderMethod = BfOrderType.Simple,
                MinuteToExpire = minuteToExpire,
                TimeInForce = timeInForce,
                Parameters = new () { BfParentOrderRequestParameter.Stop(productCode, side, triggerPrice, size) }
            };
        }

        public static BfParentOrderRequest StopLimit(BfProductCode productCode, BfTradeSide side, decimal triggerPrice, decimal orderPrice, decimal size, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            return new ()
            {
                OrderMethod = BfOrderType.Simple,
                MinuteToExpire = minuteToExpire,
                TimeInForce = timeInForce,
                Parameters = new () { BfParentOrderRequestParameter.StopLimit(productCode, side, triggerPrice, orderPrice, size) }
            };
        }

        public static BfParentOrderRequest Trail(BfProductCode productCode, BfTradeSide side, decimal offset, decimal size, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            return new ()
            {
                OrderMethod = BfOrderType.Simple,
                MinuteToExpire = minuteToExpire,
                TimeInForce = timeInForce,
                Parameters = new () { BfParentOrderRequestParameter.Trail(productCode, side, offset, size) }
            };
        }

        public static BfParentOrderRequest IFD(BfParentOrderRequestParameter first, BfParentOrderRequestParameter second, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            return new ()
            {
                OrderMethod = BfOrderType.IFD,
                MinuteToExpire = minuteToExpire,
                TimeInForce = timeInForce,
                Parameters = new () { first, second }
            };
        }

        public static BfParentOrderRequest OCO(BfParentOrderRequestParameter first, BfParentOrderRequestParameter second, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            if (first.ConditionType == second.ConditionType && first.Side == second.Side)
            {
                throw new ArgumentException("OCO child orders should not be same."); // Ordering limitations will start at Dec/2/2020
            }
            return new ()
            {
                OrderMethod = BfOrderType.OCO,
                MinuteToExpire = minuteToExpire,
                TimeInForce = timeInForce,
                Parameters = new () { first, second }
            };
        }

        public static BfParentOrderRequest IFDOCO(BfParentOrderRequestParameter ifdone, BfParentOrderRequestParameter ocoFirst, BfParentOrderRequestParameter ocoSecond, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            if (ocoFirst.ConditionType == ocoSecond.ConditionType && ocoFirst.Side == ocoSecond.Side)
            {
                throw new ArgumentException("OCO child orders should not be same."); // Ordering limitations will start at Dec/2/2020
            }
            return new ()
            {
                OrderMethod = BfOrderType.IFDOCO,
                MinuteToExpire = minuteToExpire,
                TimeInForce = timeInForce,
                Parameters = new () { ifdone, ocoFirst, ocoSecond }
            };
        }
    }

    public class BfParentOrderResponse
    {
        [JsonProperty(PropertyName = "parent_order_acceptance_id")]
        public string ParentOrderAcceptanceId { get; private set; }
    }

    public partial class BitFlyerClient
    {
        void Validate(ref BfParentOrderRequest request)
        {
            if (!request.OrderMethod.IsOrderMethod())
            {
                throw new ArgumentException();
            }
            foreach (var childOrder in request.Parameters)
            {
                if (!childOrder.ConditionType.IsConditionType())
                {
                    throw new ArgumentException();
                }
            }

            if (request.MinuteToExpire == 0 && Config.MinuteToExpire > 0)
            {
                request.MinuteToExpire = Config.MinuteToExpire;
            }

            if (request.TimeInForce == BfTimeInForce.NotSpecified && Config.TimeInForce != BfTimeInForce.NotSpecified)
            {
                request.TimeInForce = Config.TimeInForce;
            }
        }

        /// <summary>
        /// Submit New Parent Order (Special order)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/SendParentOrder">Online help</see>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public BitFlyerResponse<BfParentOrderResponse> SendParentOrder(BfParentOrderRequest request)
        {
            Validate(ref request);
            return PrivatePostAsync<BfParentOrderResponse>(nameof(SendParentOrder), request, CancellationToken.None).Result;
        }

        /// <summary>
        /// Submit New Parent Order (Special order)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/SendParentOrder">Online help</see>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BitFlyerResponse<BfParentOrderResponse>> SendParentOrderAsync(BfParentOrderRequest request, CancellationToken ct)
        {
            Validate(ref request);
            return await PrivatePostAsync<BfParentOrderResponse>(nameof(SendParentOrder), request, ct);
        }
    }
}
