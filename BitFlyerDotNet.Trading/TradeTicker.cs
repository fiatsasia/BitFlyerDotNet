//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class TradeTicker
    {
        public decimal Ask { get; set; }
        public decimal Bid { get; set; }
        public decimal TradePrice { get; set; }
        public DateTime UpdatedTime { get; set; }
        public decimal SFDDifference { get; set; }
        public decimal SFDRate { get; set; }
        public BfBoardHealth  ServerBusyStatus { get; set; }
    }
}
