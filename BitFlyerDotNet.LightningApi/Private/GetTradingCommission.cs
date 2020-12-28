//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using Newtonsoft.Json;

namespace BitFlyerDotNet.LightningApi
{
    public class BfTradingCommission
    {
        [JsonProperty(PropertyName = "commission_rate")]
        public decimal CommissionRate { get; private set; }
    }

    public partial class BitFlyerClient
    {
        /// <summary>
        /// Get Trading Commission
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetTradingCommission">Online help</see>
        /// </summary>
        /// <param name="productCode"></param>
        /// <returns></returns>
        public BitFlyerResponse<BfTradingCommission> GetTradingCommission(BfProductCode productCode)
        {
            return GetPrivateAsync<BfTradingCommission>(nameof(GetTradingCommission), "product_code=" + productCode.ToEnumString()).Result;
        }
    }
}
