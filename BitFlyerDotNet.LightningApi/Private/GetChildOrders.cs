//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfChildOrder
    {
        [JsonProperty(PropertyName = "id")]
        public int PagingId { get; private set; }

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
        public BitFlyerResponse<BfChildOrder[]> GetChildOrders(
            BfProductCode productCode,
            BfOrderState orderState = BfOrderState.Unknown,
            int count = 0,
            int before = 0,
            int after = 0,
            string childOrderId = null,
            string childOrderAcceptanceId = null,
            string parentOrderId = null
        )
        {
            var query = string.Format("product_code={0}{1}{2}{3}{4}{5}{6}{7}",
                productCode.ToEnumString(),
                orderState != BfOrderState.Unknown ? "&child_order_state=" + orderState.ToEnumString() : "",
                (count > 0)  ? $"&count={count}"   : "",
                (before > 0) ? $"&before={before}" : "",
                (after > 0)  ? $"&after={after}"   : "",
                !string.IsNullOrEmpty(childOrderId) ? "&child_order_id=" + childOrderId : "",
                !string.IsNullOrEmpty(childOrderAcceptanceId) ? "&child_order_acceptance_id=" + childOrderAcceptanceId : "",
                !string.IsNullOrEmpty(parentOrderId) ? "&parent_order_id=" + parentOrderId : ""
            );

            return PrivateGet<BfChildOrder[]>(nameof(GetChildOrders), query);
        }
    }
}
