﻿//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfMarketHealth
    {
        [JsonProperty(PropertyName = "status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfHealthState Status { get; private set; }
    }

    public partial class BitFlyerClient
    {
        /// <summary>
        /// Exchange status
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetMarketHealth">Online help</see>
        /// </summary>
        /// <param name="productCode"></param>
        /// <returns></returns>
        public BitFlyerResponse<BfMarketHealth> GetHealth(BfProductCode productCode)
        {
            return GetAsync<BfMarketHealth>(nameof(GetHealth), "product_code=" + productCode.ToEnumString()).Result;
        }

        public Task<BitFlyerResponse<BfMarketHealth>> GetHealthAsync(BfProductCode productCode)
        {
            return GetAsync<BfMarketHealth>(nameof(GetHealth), "product_code=" + productCode.ToEnumString());
        }
    }
}
