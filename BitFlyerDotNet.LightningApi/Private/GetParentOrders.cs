//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfParentOrder
    {
        [JsonProperty(PropertyName = "id")]
        public int PagingId { get; private set; }

        [JsonProperty(PropertyName = "parent_order_id")]
        public string ParentOrderId { get; private set; }

        [JsonProperty(PropertyName = "product_code")]
        public string ProductCode { get; private set; }

        [JsonProperty(PropertyName = "side")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTradeSide Side { get; private set; }

        [JsonProperty(PropertyName = "parent_order_type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderType ParentOrderType { get; private set; }

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
        public BitFlyerResponse<BfParentOrder[]> GetParentOrders(BfProductCode productCode, BfOrderState orderState = BfOrderState.Unknown, int count = 0, int before = 0, int after = 0)
        {
            var query = string.Format("product_code={0}{1}{2}{3}",
                productCode.ToEnumString(),
                orderState != BfOrderState.Unknown ? "&parent_order_state=" + orderState.ToEnumString() : "",
                (count > 0)  ? $"&count={count}"   : "",
                (before > 0) ? $"&before={before}" : "",
                (after > 0)  ? $"&after={after}"   : ""
            );

            return PrivateGet<BfParentOrder[]>(nameof(GetParentOrders), query);
        }
    }
}
