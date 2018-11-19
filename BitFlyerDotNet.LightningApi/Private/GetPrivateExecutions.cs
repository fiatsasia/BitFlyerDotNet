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
    public class BfPrivateExecution : IBfExecution
    {
        [JsonProperty(PropertyName = "id")]
        public int ExecutionId { get; private set; }

        [JsonProperty(PropertyName = "child_order_id")]
        public string ChildOrderId { get; private set; }

        [JsonProperty(PropertyName = "side")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTradeSide Side { get; private set; }

        [JsonProperty(PropertyName = "price")]
        public double Price { get; private set; }

        [JsonProperty(PropertyName = "size")]
        public double Size { get; private set; }

        [JsonProperty(PropertyName = "commission")]
        public double Commission { get; private set; }

        [JsonProperty(PropertyName = "exec_date")]
        public DateTime ExecutedTime { get; private set; }

        [JsonProperty(PropertyName = "child_order_acceptance_id")]
        public string ChildOrderAcceptanceId { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfPrivateExecution[]> GetPrivateExecutions(BfProductCode productCode, int count = 0, int before = 0, int after = 0, string childOrderId = null, string childOrderAcceptanceId = null)
        {
            var query = string.Format("product_code={0}{1}{2}{3}{4}{5}",
                productCode.ToEnumString(),
                (count > 0) ? string.Format("&count={0}", count) : "",
                (before > 0) ? string.Format("&before={0}", before) : "",
                (after > 0) ? string.Format("&after={0}", after) : "",
                !string.IsNullOrEmpty(childOrderId) ? "&child_order_id=" + childOrderId : "",
                !string.IsNullOrEmpty(childOrderAcceptanceId) ? "&child_order_acceptance_id=" + childOrderAcceptanceId : ""
            );

            return PrivateGet<BfPrivateExecution[]>("getexecutions", query);
        }
    }
}
