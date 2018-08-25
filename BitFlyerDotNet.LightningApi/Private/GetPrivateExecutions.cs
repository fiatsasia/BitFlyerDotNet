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
        const string GetExecutionsMethod = "getexecutions";

        public BitFlyerResponse<BfPrivateExecution[]> GetPrivateExecutions(string productCode)
        {
            var p = string.Format("product_code={0}",  productCode);
            return PrivateGet<BfPrivateExecution[]>(GetExecutionsMethod, p);
        }

        public BitFlyerResponse<BfPrivateExecution[]> GetPrivateExecutions(BfProductCode productCode)
        {
            return GetPrivateExecutions(productCode.ToEnumString());
        }

        public BitFlyerResponse<BfPrivateExecution[]> GetPrivateExecutionsByAcceptanceId(string productCode, string childOrderAcceptanceId)
        {
            var p = string.Format("product_code={0}&child_order_acceptance_id={1}", productCode, childOrderAcceptanceId);
            return PrivateGet<BfPrivateExecution[]>(GetExecutionsMethod, p);
        }

        public BitFlyerResponse<BfPrivateExecution[]> GetPrivateExecutionsByAcceptanceId(BfProductCode productCode, string childOrderAcceptanceId)
        {
            return GetPrivateExecutionsByAcceptanceId(productCode.ToEnumString(), childOrderAcceptanceId);
        }
    }
}
