﻿//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Newtonsoft.Json;

namespace BitFlyerDotNet.LightningApi
{
    public class BfBoardOrder
    {
        [JsonProperty(PropertyName = "price")]
        public decimal Price { get; private set; }

        [JsonProperty(PropertyName = "size")]
        public decimal Size { get; private set; }
    }

    public class BfBoard
    {
        [JsonProperty(PropertyName = "mid_price")]
        public decimal MidPrice { get; private set; }

        [JsonProperty(PropertyName = "bids")]
        public BfBoardOrder[] Bids { get; private set; }

        [JsonProperty(PropertyName = "asks")]
        public BfBoardOrder[] Asks { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfBoard> GetBoard(BfProductCode productCode)
        {
            return Get<BfBoard>(nameof(GetBoard), "product_code=" + productCode.ToEnumString());
        }
    }
}
