using System;
using System.Collections.Generic;
using System.Text;

namespace BitFlyerDotNet.Trading
{
    public class BfxMarketDataSource
    {
        public decimal BestBid { get; }
        public decimal BestAsk { get; }
        public decimal LastTradedPrice { get; }
    }
}
