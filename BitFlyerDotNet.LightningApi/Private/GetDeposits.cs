//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfDeposit
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
        public BitFlyerResponse<BfDeposit[]> GetDeposits(int count = 0, int before = 0, int after = 0)
        {
            var query = string.Format("{0}{1}{2}",
                (count > 0)  ? $"&count={count}"   : "",
                (before > 0) ? $"&before={before}" : "",
                (after > 0)  ? $"&after={after}"   : ""
            ).TrimStart('&');

            return PrivateGet<BfDeposit[]>(nameof(GetDeposits), query);
        }
    }
}
