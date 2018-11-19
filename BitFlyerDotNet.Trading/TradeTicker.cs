//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Text;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class TradeTicker
    {
        public double Ask { get; set; }
        public double Bid { get; set; }
        public double TradePrice { get; set; }
        public DateTime UpdatedTime { get; set; }
        public double SFDDifference { get; set; }
        public double SFDRate { get; set; }
        public BfBoardHealth  ServerBusyStatus { get; set; }
    }
}
