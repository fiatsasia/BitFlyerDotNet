//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfCoinAddress
    {
        [JsonProperty(PropertyName = "type")]
        public string AddressType { get; private set; }

        [JsonProperty(PropertyName = "currency_code")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfCurrencyCode CurrencyCode { get; private set; }

        [JsonProperty(PropertyName = "address")]
        public string Address { get; private set; }
    }

    public partial class BitFlyerClient
    {
        const string GetAddressesMethod = "getaddresses";

        public BitFlyerResponse<BfCoinAddress[]> GetCoinAddresses()
        {
            return PrivateGet<BfCoinAddress[]>(GetAddressesMethod);
        }
    }
}
