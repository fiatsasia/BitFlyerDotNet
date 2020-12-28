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
        /// <summary>
        /// Order Book
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetBoard">Online help</see>
        /// </summary>
        /// <param name="productCode"></param>
        /// <returns></returns>
        public BitFlyerResponse<BfBoard> GetBoard(BfProductCode productCode)
        {
            return GetAsync<BfBoard>(nameof(GetBoard), "product_code=" + productCode.ToEnumString()).Result;
        }
    }
}
