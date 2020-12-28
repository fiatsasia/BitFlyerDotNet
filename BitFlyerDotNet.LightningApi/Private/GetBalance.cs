﻿//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

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
        public decimal Amount { get; private set; }

        [JsonProperty(PropertyName = "available")]
        public decimal Available { get; private set; }
    }

    public partial class BitFlyerClient
    {
        /// <summary>
        /// Get Account Asset Balance
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetBalance">Online help</see>
        /// </summary>
        /// <returns></returns>
        public BitFlyerResponse<BfBalance[]> GetBalance()
        {
            return GetPrivateAsync<BfBalance[]>(nameof(GetBalance)).Result;
        }
    }
}
