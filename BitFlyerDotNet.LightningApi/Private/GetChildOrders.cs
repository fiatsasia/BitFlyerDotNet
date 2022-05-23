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
    public class BfaChildOrder
    {
        [JsonProperty(PropertyName = "id")]
        public uint PagingId { get; private set; }

        [JsonProperty(PropertyName = "child_order_id")]
        public string ChildOrderId { get; private set; }

        [JsonProperty(PropertyName = "product_code")]
        public string ProductCode { get; private set; }

        [JsonProperty(PropertyName = "side")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTradeSide Side { get; private set; }

        [JsonProperty(PropertyName = "child_order_type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderType ChildOrderType { get; private set; }

        [JsonProperty(PropertyName = "price")]
        public decimal Price { get; private set; } // value is 0 when executed by market price

        [JsonProperty(PropertyName = "average_price")]
        public decimal AveragePrice { get; private set; }

        [JsonProperty(PropertyName = "size")]
        public decimal Size { get; private set; }

        [JsonProperty(PropertyName = "child_order_state")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderState ChildOrderState { get; private set; }

        [JsonProperty(PropertyName = "expire_date")]
        public DateTime ExpireDate { get; private set; }

        [JsonProperty(PropertyName = "child_order_date")]
        public DateTime ChildOrderDate { get; private set; }

        [JsonProperty(PropertyName = "child_order_acceptance_id")]
        public string ChildOrderAcceptanceId { get; private set; }

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
        /// List Orders
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChildOrders">Online help</see>
        /// </summary>
        /// <param name="productCode"></param>
        /// <param name="orderState"></param>
        /// <param name="count"></param>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <param name="childOrderId"></param>
        /// <param name="childOrderAcceptanceId"></param>
        /// <param name="parentOrderId"></param>
        /// <returns></returns>
        public Task<BitFlyerResponse<BfaChildOrder[]>> GetChildOrdersAsync(
            string productCode,
            BfOrderState orderState,
            int count,
            uint before,
            uint after,
            string childOrderId,
            string childOrderAcceptanceId,
            string parentOrderId,
            CancellationToken ct
        )
        {
            var query = string.Format("product_code={0}{1}{2}{3}{4}{5}{6}{7}",
                productCode,
                orderState != BfOrderState.Unknown ? "&child_order_state=" + orderState.ToEnumString() : "",
                (count > 0)  ? $"&count={count}"   : "",
                (before > 0) ? $"&before={before}" : "",
                (after > 0)  ? $"&after={after}"   : "",
                !string.IsNullOrEmpty(childOrderId) ? "&child_order_id=" + childOrderId : "",
                !string.IsNullOrEmpty(childOrderAcceptanceId) ? "&child_order_acceptance_id=" + childOrderAcceptanceId : "",
                !string.IsNullOrEmpty(parentOrderId) ? "&parent_order_id=" + parentOrderId : ""
            );

            return GetPrivateAsync<BfaChildOrder[]>(nameof(GetChildOrders), query, ct);
        }

        public Task<BitFlyerResponse<BfaChildOrder[]>> GetChildOrdersAsync(
            string productCode,
            BfOrderState orderState = BfOrderState.Unknown,
            int count = 0,
            uint before = 0,
            uint after = 0,
            string childOrderId = null,
            string childOrderAcceptanceId = null,
            string parentOrderId = null
        )
            => GetChildOrdersAsync(productCode, orderState, count, before, after, childOrderId, childOrderAcceptanceId, parentOrderId, CancellationToken.None);

        public BitFlyerResponse<BfaChildOrder[]> GetChildOrders(
            string productCode,
            BfOrderState orderState = BfOrderState.Unknown,
            int count = 0,
            uint before = 0,
            uint after = 0,
            string childOrderId = null,
            string childOrderAcceptanceId = null,
            string parentOrderId = null
        )
            => GetChildOrdersAsync(productCode, orderState, count, before, after, childOrderId, childOrderAcceptanceId, parentOrderId, CancellationToken.None).Result;

        public IEnumerable<BfaChildOrder> GetChildOrders(string productCode, BfOrderState orderState, uint before, Func<BfaChildOrder, bool> predicate)
        {
            while (true)
            {
                var orders = GetChildOrders(productCode, orderState, ReadCountMax, before).GetContent();
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

        public IEnumerable<BfaChildOrder> GetChildOrders(string productCode, DateTime after) =>
            GetChildOrders(productCode, BfOrderState.Active, 0, e => e.ChildOrderDate >= after)
            .Concat(GetChildOrders(productCode, BfOrderState.Completed, 0, e => e.ChildOrderDate >= after))
            .Concat(GetChildOrders(productCode, BfOrderState.Canceled, 0, e => e.ChildOrderDate >= after))
            .Concat(GetChildOrders(productCode, BfOrderState.Expired, 0, e => e.ChildOrderDate >= after))
            .Concat(GetChildOrders(productCode, BfOrderState.Rejected, 0, e => e.ChildOrderDate >= after))
            .OrderByDescending(e => e.PagingId);

        public async Task<IBfChildOrder[]> GetActiveIndependentChildOrders(string productCode)
        {
            var activeDescendants = (await Task.WhenAll(
                (await GetParentOrdersAsync(productCode, orderState: BfOrderState.Active)).GetContent().Select(
                    async parent => (await GetChildOrdersAsync(productCode, parentOrderId: parent.ParentOrderId)).GetContent()
                )
            )).SelectMany(e => e);
            var activeChildren = (await GetChildOrdersAsync(productCode, orderState: BfOrderState.Active)).GetContent();

            return await Task.WhenAll(
                activeChildren
                .Where(e => !activeDescendants.Any(f => e.ChildOrderAcceptanceId == f.ChildOrderAcceptanceId))
                .Select(async e => new BfChildOrder(
                    e,
                    (await GetPrivateExecutionsAsync(productCode, childOrderAcceptanceId: e.ChildOrderAcceptanceId)).GetContent()
                ))
            );
        }
    }
}
