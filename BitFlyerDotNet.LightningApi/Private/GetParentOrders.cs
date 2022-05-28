//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfParentOrderStatus
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
        public decimal? Price { get; private set; }

        [JsonProperty(PropertyName = "average_price")]
        public decimal? AveragePrice { get; private set; }

        [JsonProperty(PropertyName = "size")]
        public decimal? Size { get; private set; }

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
        public Task<BitFlyerResponse<BfParentOrderStatus[]>> GetParentOrdersAsync(string productCode, BfOrderState orderState, int count, uint before, uint after, CancellationToken ct)
        {
            var query = string.Format("product_code={0}{1}{2}{3}",
                productCode,
                orderState != BfOrderState.Unknown ? "&parent_order_state=" + orderState.ToEnumString() : "",
                (count > 0)  ? $"&count={count}"   : "",
                (before > 0) ? $"&before={before}" : "",
                (after > 0)  ? $"&after={after}"   : ""
            );

            return GetPrivateAsync<BfParentOrderStatus[]>(nameof(GetParentOrders), query, ct);
        }

        public Task<BitFlyerResponse<BfParentOrderStatus[]>> GetParentOrdersAsync(
            string productCode,
            BfOrderState orderState = BfOrderState.Unknown,
            int count = 0,
            uint before = 0,
            uint after = 0
        )
            => GetParentOrdersAsync(productCode, orderState, count, before, after, CancellationToken.None);

        public BitFlyerResponse<BfParentOrderStatus[]> GetParentOrders(string productCode, BfOrderState orderState = BfOrderState.Unknown, int count = 0, uint before = 0, uint after = 0)
            => GetParentOrdersAsync(productCode, orderState, count, before, after).Result;

        public IEnumerable<BfParentOrderStatus> GetParentOrders(string productCode, BfOrderState orderState, uint before, Func<BfParentOrderStatus, bool> predicate)
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

        public IEnumerable<BfParentOrderStatus> GetParentOrders(string productCode, DateTime after)
            => GetParentOrders(productCode, BfOrderState.Unknown, 0, e => e.ParentOrderDate >= after);

        public BfParentOrderStatus GetParentOrder(string productCode, BfParentOrderDetailStatus detail)
        {
            var result = GetParentOrders(productCode, count: 1, before: detail.PagingId + 1).GetContent();
            if (result.Length == 0)
            {
                throw new KeyNotFoundException();
            }
            return result[0];
        }

        public BfParentOrderStatus GetParentOrder(string productCode, string parentOrderAcceptanceId = null, string parentOrderId = null)
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
