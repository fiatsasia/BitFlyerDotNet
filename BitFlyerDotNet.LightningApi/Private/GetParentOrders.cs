//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfaParentOrder
    {
        [JsonProperty(PropertyName = "id")]
        public uint PagingId { get; private set; }

        [JsonProperty(PropertyName = "parent_order_id")]
        public string ParentOrderId { get; private set; }

        [JsonProperty(PropertyName = "product_code")]
        public string ProductCode { get; private set; }

        [JsonProperty(PropertyName = "side")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTradeSide Side { get; private set; }

        [JsonProperty(PropertyName = "parent_order_type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderType ParentOrderType { get; private set; } // if request is simple, this contains children[0]

        [JsonProperty(PropertyName = "price")]
        public decimal Price { get; private set; } // value is 0 when executed by market price

        [JsonProperty(PropertyName = "average_price")]
        public decimal AveragePrice { get; private set; }

        [JsonProperty(PropertyName = "size")]
        public decimal Size { get; private set; }

        [JsonProperty(PropertyName = "parent_order_state")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderState ParentOrderState { get; private set; }

        [JsonProperty(PropertyName = "expire_date")]
        public DateTime ExpireDate { get; private set; }

        [JsonProperty(PropertyName = "parent_order_date")]
        public DateTime ParentOrderDate { get; private set; }

        [JsonProperty(PropertyName = "parent_order_acceptance_id")]
        public string ParentOrderAcceptanceId { get; private set; }

        [JsonProperty(PropertyName = "outstanding_size")]
        public decimal OutstandingSize { get; private set; }

        [JsonProperty(PropertyName = "cancel_size")]
        public decimal CancelSize { get; private set; }

        [JsonProperty(PropertyName = "executed_size")]
        public decimal ExecutedSize { get; private set; }

        [JsonProperty(PropertyName = "total_commission")]
        public decimal TotalCommission { get; private set; }
    }

    public partial class BitFlyerClient
    {
        /// <summary>
        /// List Parent Orders
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetParentOrders">Online help</see>
        /// </summary>
        /// <param name="productCode"></param>
        /// <param name="orderState"></param>
        /// <param name="count"></param>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <returns></returns>
        public BitFlyerResponse<BfaParentOrder[]> GetParentOrders(BfProductCode productCode, BfOrderState orderState = BfOrderState.Unknown, int count = 0, uint before = 0, uint after = 0)
        {
            var query = string.Format("product_code={0}{1}{2}{3}",
                productCode.ToEnumString(),
                orderState != BfOrderState.Unknown ? "&parent_order_state=" + orderState.ToEnumString() : "",
                (count > 0)  ? $"&count={count}"   : "",
                (before > 0) ? $"&before={before}" : "",
                (after > 0)  ? $"&after={after}"   : ""
            );

            return PrivateGetAsync<BfaParentOrder[]>(nameof(GetParentOrders), query).Result;
        }

        public IEnumerable<BfaParentOrder> GetParentOrders(BfProductCode productCode, BfOrderState orderState, uint before, Func<BfaParentOrder, bool> predicate)
        {
            while (true)
            {
                var orders = GetParentOrders(productCode, orderState, ReadCountMax, before).GetContent();
                if (orders.Length == 0)
                {
                    break;
                }

                foreach (var order in orders)
                {
                    if (!predicate(order))
                    {
                        yield break;
                    }
                    yield return order;
                }

                if (orders.Length < ReadCountMax)
                {
                    break;
                }
                before = orders.Last().PagingId;
            }
        }

        public IEnumerable<BfaParentOrder> GetParentOrders(BfProductCode productCode, DateTime after)
            => GetParentOrders(productCode, BfOrderState.Unknown, 0, e => e.ParentOrderDate >= after);

        public BfaParentOrder GetParentOrder(BfProductCode productCode, BfaParentOrderDetail detail)
        {
            var result = GetParentOrders(productCode, count: 1, before: detail.PagingId + 1).GetContent();
            if (result.Length == 0)
            {
                throw new KeyNotFoundException();
            }
            return result[0];
        }

        public BfaParentOrder GetParentOrder(BfProductCode productCode, string parentOrderAcceptanceId = null, string parentOrderId = null)
        {
            if (string.IsNullOrEmpty(parentOrderAcceptanceId) && string.IsNullOrEmpty(parentOrderId))
            {
                throw new ArgumentException();
            }

            var detail = (!string.IsNullOrEmpty(parentOrderAcceptanceId)
                ? GetParentOrderDetail(productCode, parentOrderAcceptanceId: parentOrderAcceptanceId)
                : GetParentOrderDetail(productCode, parentOrderId: parentOrderId)).GetContent();

            return GetParentOrder(productCode, detail);
        }
    }
}
