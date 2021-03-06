﻿//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfaPrivateExecution : IBfExecution
    {
        [JsonProperty(PropertyName = "id")]
        public long ExecutionId { get; private set; }

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
        public Task<BitFlyerResponse<BfaPrivateExecution[]>> GetPrivateExecutionsAsync(BfProductCode productCode, int count, long before, long after, string childOrderId, string childOrderAcceptanceId, CancellationToken ct)
        {
            var query = string.Format("product_code={0}{1}{2}{3}{4}{5}",
                productCode.ToEnumString(),
                (count > 0)  ? $"&count={count}"   : "",
                (before > 0) ? $"&before={before}" : "",
                (after > 0)  ? $"&after={after}"   : "",
                !string.IsNullOrEmpty(childOrderId) ? "&child_order_id=" + childOrderId : "",
                !string.IsNullOrEmpty(childOrderAcceptanceId) ? "&child_order_acceptance_id=" + childOrderAcceptanceId : ""
            );

            return GetPrivateAsync<BfaPrivateExecution[]>("getexecutions", query, ct);
        }

        public BitFlyerResponse<BfaPrivateExecution[]> GetPrivateExecutions(BfProductCode productCode, int count = 0, long before = 0, long after = 0, string childOrderId = null, string childOrderAcceptanceId = null)
            => GetPrivateExecutionsAsync(productCode, count, before, after, childOrderId, childOrderAcceptanceId, CancellationToken.None).Result;


        public IEnumerable<BfaPrivateExecution> GetPrivateExecutions(BfProductCode productCode, long before, Func<BfaPrivateExecution, bool> predicate)
        {
            while (true)
            {
                var execs = GetPrivateExecutions(productCode, ReadCountMax, before, 0).GetContent();
                if (execs.Length == 0)
                {
                    break;
                }

                foreach (var exec in execs)
                {
                    if (!predicate(exec))
                    {
                        yield break;
                    }
                    yield return exec;
                }

                if (execs.Length < ReadCountMax)
                {
                    break;
                }
                before = execs.Last().ExecutionId;
            }
        }

        public IEnumerable<BfaPrivateExecution> GetPrivateExecutions(BfProductCode productCode, DateTime after)
            => GetPrivateExecutions(productCode, 0, e => e.ExecutedTime >= after);
    }
}
