//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfBalance
    {
        [JsonProperty(PropertyName = "currency_code")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfCurrencyCode CurrencyCode { get; private set; }

        [JsonProperty(PropertyName = "amount")]
        public double Amount { get; private set; }

        [JsonProperty(PropertyName = "available")]
        public double Available { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfBalance[]> GetBalance()
        {
            return PrivateGet<BfBalance[]>(nameof(GetBalance));
        }
    }
}
