//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

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
        [Obsolete("This method is obsolete. Use GetHealth instead.", false)]
        public BitFlyerResponse<BfMarketHealth> GetMarketHealth(BfProductCode productCode)
        {
            return Get<BfMarketHealth>("gethealth", "product_code=" + productCode.ToEnumString());
        }

        /// <summary>
        /// Exchange status
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetMarketHealth">Online help</see>
        /// </summary>
        /// <param name="productCode"></param>
        /// <returns></returns>
        public BitFlyerResponse<BfMarketHealth> GetHealth(BfProductCode productCode)
        {
            return Get<BfMarketHealth>(nameof(GetHealth), "product_code=" + productCode.ToEnumString());
        }
    }
}
