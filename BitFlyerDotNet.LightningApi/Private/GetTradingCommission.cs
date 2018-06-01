//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Fiats.Utils;

namespace BitFlyerDotNet.LightningApi
{
    public class BfTradingCommission
    {
        [JsonProperty(PropertyName = "commission_rate")]
        public double CommissionRate { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfTradingCommission> GetTradingCommission(BfProductCode productCode)
        {
            return PrivateGet<BfTradingCommission>(nameof(GetTradingCommission), string.Format("product_code={0}", productCode.ToEnumString()));
        }
    }
}
