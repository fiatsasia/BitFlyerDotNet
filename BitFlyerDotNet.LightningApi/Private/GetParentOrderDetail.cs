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
    public class BfParentOrderDetailParameter
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
        public int Offset { get; private set; }
    }

    public class BfParentOrderDetail
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; private set; }

        [JsonProperty(PropertyName = "parent_order_id")]
        public string ParentOrderId { get; private set; }

        [JsonProperty(PropertyName = "order_method")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfOrderMethod OrderMethod { get; private set; }

        [JsonProperty(PropertyName = "minute_to_expire")]
        public int MinuteToExpire { get; private set; }

        [JsonProperty(PropertyName = "parameters")]
        public BfParentOrderDetailParameter[] Parameters { get; private set; }
    }

    public partial class BitFlyerClient
    {
        const string GetParentOrderMethod = "getparentorder";

        public BitFlyerResponse<BfParentOrderDetail> GetParentOrderDetailByAcceptanceId(string productCode, string parentOrderAcceptanceId)
        {
            var p = string.Format("product_code={0}&parent_order_acceptance_id={1}", productCode, parentOrderAcceptanceId);
            return PrivateGet<BfParentOrderDetail>(GetParentOrderMethod, p);
        }

        public BitFlyerResponse<BfParentOrderDetail> GetParentOrderDetailByAcceptanceId(BfProductCode productCode, string parentOrderAcceptanceId)
        {
            return GetParentOrderDetailByAcceptanceId(productCode.ToEnumString(), parentOrderAcceptanceId);
        }

        public BitFlyerResponse<BfParentOrderDetail> GetParentOrderDetailByOrderId(string productCode, string parentOrderId)
        {
            var p = string.Format("product_code={0}&parent_order_id={1}", productCode, parentOrderId);
            return PrivateGet<BfParentOrderDetail>(GetParentOrderMethod, p);
        }

        public BitFlyerResponse<BfParentOrderDetail> GetParentOrderDetailByOrderId(BfProductCode productCode, string parentOrderId)
        {
            return GetParentOrderDetailByOrderId(productCode.ToEnumString(), parentOrderId);
        }
    }
}
