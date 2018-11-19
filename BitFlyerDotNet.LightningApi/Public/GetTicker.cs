//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using Newtonsoft.Json;
using Fiats.Utils;

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
        public double BestBid { get; private set; }

        [JsonProperty(PropertyName = "best_ask")]
        public double BestAsk { get; private set; }

        [JsonProperty(PropertyName = "best_bid_size")]
        public double BestBidSize { get; private set; }

        [JsonProperty(PropertyName = "best_ask_size")]
        public double BestAskSize { get; private set; }

        [JsonProperty(PropertyName = "total_bid_depth")]
        public double TotalBidDepth { get; private set; }

        [JsonProperty(PropertyName = "total_ask_depth")]
        public double TotalAskDepth { get; private set; }

        [JsonProperty(PropertyName = "ltp")]
        public double LastTradedPrice { get; private set; }

        [JsonProperty(PropertyName = "volume")]
        public double Last24HoursVolume { get; private set; }

        [JsonProperty(PropertyName = "volume_by_product")]
        public double VolumeByProduct { get; private set; }

        public double MidPrice { get { return (BestAsk + BestBid) / 2.0; } }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfTicker> GetTicker(BfProductCode productCode)
        {
            return Get<BfTicker>(nameof(GetTicker), "product_code=" + productCode.ToEnumString());
        }
    }
}
