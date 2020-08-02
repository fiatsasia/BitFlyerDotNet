//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
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
        public BitFlyerResponse<BfCollateralHistory[]> GetCollateralHistory(int count = 0, int before = 0, int after = 0)
        {
            var query = string.Format("{0}{1}{2}",
                (count > 0)  ? $"&count={count}"   : "",
                (before > 0) ? $"&before={before}" : "",
                (after > 0)  ? $"&after={after}"   : ""
            ).TrimStart('&');

            return PrivateGetAsync<BfCollateralHistory[]>(nameof(GetCollateralHistory), query).Result;
        }
    }
}
