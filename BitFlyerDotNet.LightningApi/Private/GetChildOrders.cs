//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Fiats.Utils;

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
        public double Price { get; private set; } // value is 0 when executed by market price

        [JsonProperty(PropertyName = "average_price")]
        public double AveragePrice { get; private set; }

        [JsonProperty(PropertyName = "size")]
        public double Size { get; private set; }

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
        public double OutstandingSize { get; private set; }

        [JsonProperty(PropertyName = "cancel_size")]
        public double CancelSize { get; private set; }

        [JsonProperty(PropertyName = "executed_size")]
        public double ExecutedSize { get; private set; }

        [JsonProperty(PropertyName = "total_commission")]
        public double TotalCommission { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfChildOrder[]> GetChildOrders(string productCode, int count = 0, int before = 0, int after = 0)
        {
            var query = string.Format("product_code={0}{1}{2}{3}",
                productCode,
                (count > 0) ? string.Format("&count={0}", count) : "",
                (before > 0) ? string.Format("&before={0}", before) : "",
                (after > 0) ? string.Format("&after={0}", after) : ""
            );
            return PrivateGet<BfChildOrder[]>(nameof(GetChildOrders), query);
        }

        public BitFlyerResponse<BfChildOrder[]> GetChildOrders(BfProductCode productCode, int count = 0, int before = 0, int after = 0)
        {
            return GetChildOrders(productCode.ToEnumString(), count, before, after);
        }

        public BitFlyerResponse<BfChildOrder[]> GetChildOrders(string productCode, BfOrderState orderState, int count = 0, int before = 0, int after = 0)
        {
            var query = string.Format("product_code={0}&child_order_state={1}{2}{3}{4}",
                productCode,
                orderState.ToEnumString(),
                (count > 0) ? string.Format("&count={0}", count) : "",
                (before > 0) ? string.Format("&before={0}", before) : "",
                (after > 0) ? string.Format("&after={0}", after) : ""
            );
            return PrivateGet<BfChildOrder[]>(nameof(GetChildOrders), query);
        }

        public BitFlyerResponse<BfChildOrder[]> GetChildOrders(BfProductCode productCode, BfOrderState orderState, int count = 0, int before = 0, int after = 0)
        {
            return GetChildOrders(productCode.ToEnumString(), orderState, count, before, after);
        }

        public BitFlyerResponse<BfChildOrder[]> GetChildOrdersByAcceptanceId(string productCode, string childOrderAcceptanceId)
        {
            var p = string.Format("product_code={0}&child_order_acceptance_id={1}", productCode, childOrderAcceptanceId);
            return PrivateGet<BfChildOrder[]>(nameof(GetChildOrders), p);
        }

        public BitFlyerResponse<BfChildOrder[]> GetChildOrdersByAcceptanceId(BfProductCode productCode, string childOrderAcceptanceId)
        {
            return GetChildOrdersByAcceptanceId(productCode.ToEnumString(), childOrderAcceptanceId);
        }

        public BitFlyerResponse<BfChildOrder[]> GetChildOrdersByOrderId(string productCode, string childOrderId)
        {
            var p = string.Format("product_code={0}&child_order_id={1}", productCode, childOrderId);
            return PrivateGet<BfChildOrder[]>(nameof(GetChildOrders), p);
        }

        public BitFlyerResponse<BfChildOrder[]> GetChildOrdersByOrderId(BfProductCode productCode, string childOrderId)
        {
            return GetChildOrdersByOrderId(productCode.ToEnumString(), childOrderId);
        }

        public BitFlyerResponse<BfChildOrder[]> GetChildOrdersByParentOrderId(string productCode, string parentOrderId)
        {
            var p = string.Format("product_code={0}&parent_order_id={1}", productCode, parentOrderId);
            return PrivateGet<BfChildOrder[]>(nameof(GetChildOrders), p);
        }

        public BitFlyerResponse<BfChildOrder[]> GetChildOrdersByParentOrderId(BfProductCode productCode, string parentOrderId)
        {
            return GetChildOrdersByParentOrderId(productCode.ToEnumString(), parentOrderId);
        }
    }
}
