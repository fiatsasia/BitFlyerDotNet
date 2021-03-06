﻿//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfaExecution : IBfExecution
    {
        [JsonProperty(PropertyName = "id")]
        public long ExecutionId { get; private set; }

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

        // Compatobility for BfPrivateExecution
        public string ChildOrderAcceptanceId { get { return Side == BfTradeSide.Buy ? BuyChildOrderAcceptanceId : SellChildOrderAcceptanceId; } }
    }

    public partial class BitFlyerClient
    {
        /// <summary>
        /// Execution History
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetExecutions">Online help</see>
        /// </summary>
        /// <param name="productCode"></param>
        /// <param name="count"></param>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <returns></returns>
        public Task<BitFlyerResponse<BfaExecution[]>> GetExecutionsAsync(BfProductCode productCode, long count, long before, long after, CancellationToken ct)
        {
            var query = string.Format("product_code={0}{1}{2}{3}",
                productCode.ToEnumString(),
                (count > 0) ? $"&count={count}" : "",
                (before > 0) ? $"&before={before}" : "",
                (after > 0) ? $"&after={after}" : ""
            );
            return GetAsync<BfaExecution[]>(nameof(GetExecutions), query, ct);
        }

        public BitFlyerResponse<BfaExecution[]> GetExecutions(BfProductCode productCode, long count = 0, long before = 0, long after = 0)
            => GetExecutionsAsync(productCode, count, before, after, CancellationToken.None).Result;
    }
}
