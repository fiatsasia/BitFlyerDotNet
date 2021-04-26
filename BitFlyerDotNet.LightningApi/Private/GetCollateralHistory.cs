//==============================================================================
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
    public class BfCollateralHistory
    {
        [JsonProperty(PropertyName = "id")]
        public int PagingId { get; private set; }

        [JsonProperty(PropertyName = "currency_code")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfCurrencyCode CurrencyCode { get; private set; }

        [JsonProperty(PropertyName = "change")]
        public decimal Change { get; private set; }

        [JsonProperty(PropertyName = "amount")]
        public decimal Amount { get; private set; }

        [JsonProperty(PropertyName = "reason_code")]
        public string ReasonCode { get; private set; }

        [JsonProperty(PropertyName = "date")]
        public DateTime Date { get; private set; }
    }

    public partial class BitFlyerClient
    {
        /// <summary>
        /// Get Margin Change History
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetCollateralHistory">Online help</see>
        /// </summary>
        /// <param name="count"></param>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <returns></returns>
        public Task<BitFlyerResponse<BfCollateralHistory[]>> GetCollateralHistoryAsync(int count, int before, int after, CancellationToken ct)
        {
            var query = string.Format("{0}{1}{2}",
                (count > 0)  ? $"&count={count}"   : "",
                (before > 0) ? $"&before={before}" : "",
                (after > 0)  ? $"&after={after}"   : ""
            ).TrimStart('&');

            return GetPrivateAsync<BfCollateralHistory[]>(nameof(GetCollateralHistory), query, ct);
        }

        public BitFlyerResponse<BfCollateralHistory[]> GetCollateralHistory(int count = 0, int before = 0, int after = 0)
            => GetCollateralHistoryAsync(count, before, after, CancellationToken.None).Result;

        public IEnumerable<BfCollateralHistory> GetCollateralHistory(int before, Func<BfCollateralHistory, bool> predicate)
        {
            while (true)
            {
                var execs = GetCollateralHistory(ReadCountMax, before, 0).GetContent();
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
                before = execs.Last().PagingId;
            }
        }
    }
}
