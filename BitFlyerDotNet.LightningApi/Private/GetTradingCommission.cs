//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
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
        public BitFlyerResponse<BfTradingCommission> GetTradingCommission(BfProductCode productCode)
        {
            return PrivateGet<BfTradingCommission>(nameof(GetTradingCommission), "product_code=" + productCode.ToEnumString());
        }
    }
}
