//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
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
        public double Change { get; private set; }

        [JsonProperty(PropertyName = "amount")]
        public double Amount { get; private set; }

        [JsonProperty(PropertyName = "reason_code")]
        public string ReasonCode { get; private set; }

        [JsonProperty(PropertyName = "date")]
        public DateTime Date { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfCollateralHistory[]> GetCollateralHistory(int count = 0, int before = 0, int after = 0)
        {
            var query = string.Format("{0}{1}{2}",
                (count > 0) ? string.Format("&count={0}", count) : "",
                (before > 0) ? string.Format("&before={0}", before) : "",
                (after > 0) ? string.Format("&after={0}", after) : ""
            ).TrimStart('&');

            return PrivateGet<BfCollateralHistory[]>(nameof(GetCollateralHistory), query);
        }
    }
}
