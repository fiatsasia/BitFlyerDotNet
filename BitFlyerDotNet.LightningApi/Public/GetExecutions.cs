//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Fiats.Utils;

namespace BitFlyerDotNet.LightningApi
{
    // Common interface between BfExecution and BfPrivateExecution
    public interface IBfExecution
    {
        int ExecutionId { get; }
        BfTradeSide Side { get; }
        decimal Price { get; }
        decimal Size { get; }
        DateTime ExecutedTime { get; }
        string ChildOrderAcceptanceId { get; }
    }

    public class BfExecution : IBfExecution
    {
        [JsonProperty(PropertyName = "id")]
        public int ExecutionId { get; private set; }

        [JsonProperty(PropertyName = "side")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTradeSide Side { get; private set; }

        [JsonProperty(PropertyName = "price")]
        public decimal Price { get; private set; }

        [JsonProperty(PropertyName = "size")]
        public decimal Size { get; private set; }

        [JsonProperty(PropertyName = "exec_date")]
        public DateTime ExecutedTime { get; private set; }

        [JsonProperty(PropertyName = "buy_child_order_acceptance_id")]
        public string BuyChildOrderAcceptanceId { get; private set; }

        [JsonProperty(PropertyName = "sell_child_order_acceptance_id")]
        public string SellChildOrderAcceptanceId { get; private set; }

        public string ChildOrderAcceptanceId { get { return Side == BfTradeSide.Buy ? BuyChildOrderAcceptanceId : SellChildOrderAcceptanceId; } }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfExecution[]> GetExecutions(BfProductCode productCode, int count = 0, int before = 0, int after = 0)
        {
            var query = string.Format("product_code={0}{1}{2}{3}",
                productCode.ToEnumString(),
                (count > 0)  ? $"&count={count}"   : "",
                (before > 0) ? $"&before={before}" : "",
                (after > 0)  ? $"&after={after}"   : ""
            );
            return Get<BfExecution[]>(nameof(GetExecutions), query);
        }
    }
}
