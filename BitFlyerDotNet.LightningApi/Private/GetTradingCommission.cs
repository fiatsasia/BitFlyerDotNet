//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
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
            return PrivateGet<BfTradingCommission>(nameof(GetTradingCommission), "product_code=" + productCode.ToEnumString());
        }
    }
}
