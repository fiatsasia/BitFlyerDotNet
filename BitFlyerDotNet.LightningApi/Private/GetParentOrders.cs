//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Fiats.Utils;

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
        public double Price { get; private set; } // value is 0 when executed by market price

        [JsonProperty(PropertyName = "average_price")]
        public double AveragePrice { get; private set; }

        [JsonProperty(PropertyName = "size")]
        public double Size { get; private set; }

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
        public BitFlyerResponse<BfParentOrder[]> GetParentOrders(string productCode, int count = 0, int before = 0, int after = 0)
        {
            var query = string.Format("product_code={0}{1}{2}{3}",
                productCode,
                (count > 0) ? string.Format("&count={0}", count) : "",
                (before > 0) ? string.Format("&before={0}", before) : "",
                (after > 0) ? string.Format("&after={0}", after) : ""
            );
            return PrivateGet<BfParentOrder[]>(nameof(GetParentOrders), query);
        }

        public BitFlyerResponse<BfParentOrder[]> GetParentOrders(BfProductCode productCode, int count = 0, int before = 0, int after = 0)
        {
            return GetParentOrders(productCode.ToEnumString(), count, before, after);
        }

        public BitFlyerResponse<BfParentOrder[]> GetParentOrders(string productCode, BfOrderState orderState, int count = 0, int before = 0, int after = 0)
        {
            var query = string.Format("product_code={0}&parent_order_state={1}{2}{3}{4}",
                productCode,
                orderState.ToEnumString(),
                (count > 0) ? string.Format("&count={0}", count) : "",
                (before > 0) ? string.Format("&before={0}", before) : "",
                (after > 0) ? string.Format("&after={0}", after) : ""
            );
            return PrivateGet<BfParentOrder[]>(nameof(GetParentOrders), query);
        }

        public BitFlyerResponse<BfParentOrder[]> GetParentOrders(BfProductCode productCode, BfOrderState orderState, int count = 0, int before = 0, int after = 0)
        {
            return GetParentOrders(productCode.ToEnumString(), orderState, count, before, after);
        }

        public BitFlyerResponse<BfParentOrder[]> GetParentOrdersByAcceptanceId(string productCode, string parentOrderAcceptanceId)
        {
            var p = string.Format("product_code={0}&parent_order_acceptance_id={1}", productCode, parentOrderAcceptanceId);
            return PrivateGet<BfParentOrder[]>(nameof(GetParentOrders), p);
        }

        public BitFlyerResponse<BfParentOrder[]> GetParentOrdersByAcceptanceId(BfProductCode productCode, string parentOrderAcceptanceId)
        {
            return GetParentOrdersByAcceptanceId(productCode.ToEnumString(), parentOrderAcceptanceId);
        }

        public BitFlyerResponse<BfParentOrder[]> GetParentOrdersByOrderId(string productCode, string parentOrderId)
        {
            var p = string.Format("product_code={0}&parent_order_id={1}", productCode, parentOrderId);
            return PrivateGet<BfParentOrder[]>(nameof(GetParentOrders), p);
        }

        public BitFlyerResponse<BfParentOrder[]> GetParentOrdersByOrderId(BfProductCode productCode, string parentOrderId)
        {
            return GetParentOrdersByOrderId(productCode.ToEnumString(), parentOrderId);
        }
    }
}
