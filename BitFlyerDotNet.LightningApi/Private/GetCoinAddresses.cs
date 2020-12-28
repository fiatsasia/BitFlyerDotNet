//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
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
        /// <summary>
        /// Get Crypto Assets Deposit Addresses
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetCoinAddresses">Online help</see>
        /// </summary>
        /// <returns></returns>
        [Obsolete("This method is obsolete. Use GetAddresses instead.", false)]
        public BitFlyerResponse<BfCoinAddress[]> GetCoinAddresses()
        {
            return GetPrivateAsync<BfCoinAddress[]>("getaddresses").Result;
        }

        /// <summary>
        /// Get Crypto Assets Deposit Addresses
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetCoinAddresses">Online help</see>
        /// </summary>
        /// <returns></returns>
        public BitFlyerResponse<BfCoinAddress[]> GetAddresses()
        {
            return GetPrivateAsync<BfCoinAddress[]>(nameof(GetAddresses)).Result;
        }
    }
}
