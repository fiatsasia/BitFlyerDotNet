//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
        public decimal Price { get; private set; }

        [JsonProperty(PropertyName = "size")]
        public decimal Size { get; private set; }

        [JsonProperty(PropertyName = "commission")]
        public decimal Commission { get; private set; }

        [JsonProperty(PropertyName = "exec_date")]
        public DateTime ExecutedTime { get; private set; }

        [JsonProperty(PropertyName = "child_order_acceptance_id")]
        public string ChildOrderAcceptanceId { get; private set; }
    }

    public partial class BitFlyerClient
    {
        /// <summary>
        /// List Executions
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetPrivateExecutions">Online help</see>
        /// </summary>
        /// <param name="productCode"></param>
        /// <param name="count"></param>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <param name="childOrderId"></param>
        /// <param name="childOrderAcceptanceId"></param>
        /// <returns></returns>
        public BitFlyerResponse<BfPrivateExecution[]> GetPrivateExecutions(BfProductCode productCode, int count = 0, int before = 0, int after = 0, string childOrderId = null, string childOrderAcceptanceId = null)
        {
            var query = string.Format("product_code={0}{1}{2}{3}{4}{5}",
                productCode.ToEnumString(),
                (count > 0)  ? $"&count={count}"   : "",
                (before > 0) ? $"&before={before}" : "",
                (after > 0)  ? $"&after={after}"   : "",
                !string.IsNullOrEmpty(childOrderId) ? "&child_order_id=" + childOrderId : "",
                !string.IsNullOrEmpty(childOrderAcceptanceId) ? "&child_order_acceptance_id=" + childOrderAcceptanceId : ""
            );

            return PrivateGet<BfPrivateExecution[]>("getexecutions", query);
        }
    }
}
