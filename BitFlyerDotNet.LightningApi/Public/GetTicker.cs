//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using Newtonsoft.Json;

namespace BitFlyerDotNet.LightningApi
{
    public class BfTicker
    {
        [JsonProperty(PropertyName = "product_code")]
        public string ProductCode { get; private set; }

        [JsonProperty(PropertyName = "timestamp")]
        public DateTime Timestamp { get; private set; }

        [JsonProperty(PropertyName = "tick_id")]
        public int TickId { get; private set; }

        [JsonProperty(PropertyName = "best_bid")]
        public decimal BestBid { get; private set; }

        [JsonProperty(PropertyName = "best_ask")]
        public decimal BestAsk { get; private set; }

        [JsonProperty(PropertyName = "best_bid_size")]
        public decimal BestBidSize { get; private set; }

        [JsonProperty(PropertyName = "best_ask_size")]
        public decimal BestAskSize { get; private set; }

        [JsonProperty(PropertyName = "total_bid_depth")]
        public decimal TotalBidDepth { get; private set; }

        [JsonProperty(PropertyName = "total_ask_depth")]
        public decimal TotalAskDepth { get; private set; }

        [JsonProperty(PropertyName = "ltp")]
        public decimal LastTradedPrice { get; private set; }

        [JsonProperty(PropertyName = "volume")]
        public decimal Last24HoursVolume { get; private set; }

        [JsonProperty(PropertyName = "volume_by_product")]
        public decimal VolumeByProduct { get; private set; }
    }

    public partial class BitFlyerClient
    {
        /// <summary>
        /// Ticker
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetTicker">Online help</see>
        /// </summary>
        /// <param name="productCode"></param>
        /// <returns></returns>
        public BitFlyerResponse<BfTicker> GetTicker(BfProductCode productCode)
        {
            return GetAsync<BfTicker>(nameof(GetTicker), "product_code=" + productCode.ToEnumString()).Result;
        }
    }
}
