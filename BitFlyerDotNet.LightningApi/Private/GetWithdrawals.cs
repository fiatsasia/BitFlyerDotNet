//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
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
    public class BfWithdrawal
    {
        [JsonProperty(PropertyName = "id")]
        public int PagingId { get; private set; }

        [JsonProperty(PropertyName = "order_id")]
        public string OrderId { get; private set; }

        [JsonProperty(PropertyName = "currency_code")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfCurrencyCode CurrencyCode { get; private set; }

        [JsonProperty(PropertyName = "amount")]
        public decimal Amount { get; private set; }

        [JsonProperty(PropertyName = "status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTransactionStatus Status { get; private set; }

        [JsonProperty(PropertyName = "event_date")]
        public DateTime EventDate { get; private set; }
    }

    public partial class BitFlyerClient
    {
        /// <summary>
        /// Get Deposit Cancellation History
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetWithdrawals">Online help</see>
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="count"></param>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <returns></returns>
        public Task<BitFlyerResponse<BfWithdrawal[]>> GetWithdrawalsAsync(string messageId, int count, int before, int after, CancellationToken ct)
        {
            var query = string.Format("{0}{1}{2}{3}",
                !string.IsNullOrEmpty(messageId) ? "message_id=" + messageId : "",
                (count > 0)  ? $"&count={count}"   : "",
                (before > 0) ? $"&before={before}" : "",
                (after > 0)  ? $"&after={after}"   : ""
            ).TrimStart('&');

            return GetPrivateAsync<BfWithdrawal[]>(nameof(GetWithdrawals), query, ct);
        }

        public BitFlyerResponse<BfWithdrawal[]> GetWithdrawals(string messageId = null, int count = 0, int before = 0, int after = 0)
            => GetWithdrawalsAsync(messageId, count, before, after, CancellationToken.None).Result;
    }
}
