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
    public class BfChildOrderElement
    {
        [JsonProperty(PropertyName = "product_code")]
        public string ProductCode { get; private set; }

        [JsonProperty(PropertyName = "condition_type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderType ConditionType { get; private set; }

        [JsonProperty(PropertyName = "side")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTradeSide Side { get; private set; }

        [JsonProperty(PropertyName = "price")]
        public double Price { get; private set; }

        [JsonProperty(PropertyName = "size")]
        public double Size { get; private set; }

        [JsonProperty(PropertyName = "trigger_price")]
        public double TriggerPrice { get; private set; }

        [JsonProperty(PropertyName = "offset")]
        public double Offset { get; private set; }
    }

    public class BfParentOrderDetail
    {
        [JsonProperty(PropertyName = "id")]
        public int PagingId { get; private set; }

        [JsonProperty(PropertyName = "parent_order_id")]
        public string ParentOrderId { get; private set; }

        [JsonProperty(PropertyName = "order_method")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderType OrderMethod { get; private set; }

        [JsonProperty(PropertyName = "minute_to_expire")]
        public int MinuteToExpire { get; private set; }

        [JsonProperty(PropertyName = "parameters")]
        public BfChildOrderElement[] ChildOrders { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfParentOrderDetail> GetParentOrder(BfProductCode productCode, string parentOrderId = null, string parentOrderAcceptanceId = null)
        {
            if (string.IsNullOrEmpty(parentOrderId) && string.IsNullOrEmpty(parentOrderAcceptanceId))
            {
                throw new ArgumentException();
            }

            var query = string.Format("product_code={0}{1}{2}",
                productCode.ToEnumString(),
                !string.IsNullOrEmpty(parentOrderId) ? "&parent_order_id=" + parentOrderId : "",
                !string.IsNullOrEmpty(parentOrderAcceptanceId) ? "&parent_order_acceptance_id=" + parentOrderAcceptanceId : ""
            );

            return PrivateGet<BfParentOrderDetail>(nameof(GetParentOrder), query);
        }
    }
}
