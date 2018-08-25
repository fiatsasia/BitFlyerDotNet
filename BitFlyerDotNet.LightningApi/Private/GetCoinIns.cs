//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfCoinin
    {
        [JsonProperty(PropertyName = "id")]
        public int PagingId { get; private set; }

        [JsonProperty(PropertyName = "order_id")]
        public string OrderId { get; private set; }

        [JsonProperty(PropertyName = "currency_code")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfCurrencyCode CurrencyCode { get; private set; }

        [JsonProperty(PropertyName = "amount")]
        public double Amount { get; private set; }

        [JsonProperty(PropertyName = "address")]
        public string CoinAddress { get; private set; }

        [JsonProperty(PropertyName = "tx_hash")]
        public string TransactionHash { get; private set; }

        [JsonProperty(PropertyName = "status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfTransactionStatus TransactionStatus { get; private set; }

        [JsonProperty(PropertyName = "event_date")]
        public DateTime EventDate { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfCoinin[]> GetCoinIns(int count = 0, int before = 0, int after = 0)
        {
            var query = string.Format("{0}{1}{2}",
                (count > 0) ? string.Format("&count={0}", count) : "",
                (before > 0) ? string.Format("&before={0}", before) : "",
                (after > 0) ? string.Format("&after={0}", after) : ""
            ).TrimStart('&');

            return PrivateGet<BfCoinin[]>(nameof(GetCoinIns), query);
        }
    }
}
