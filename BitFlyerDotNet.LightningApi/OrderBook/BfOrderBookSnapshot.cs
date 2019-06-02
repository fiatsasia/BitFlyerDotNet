//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System.Linq;
using System.Collections.Generic;

namespace BitFlyerDotNet.LightningApi
{
    public class BfOrderBookSnapshot
    {
        List<(decimal Price, decimal Size)> _bids = new List<(decimal Price, decimal Size)>();
        public IReadOnlyList<(decimal Price, decimal Size)> Bids => _bids;

        List<(decimal Price, decimal Size)> _asks = new List<(decimal Price, decimal Size)>();
        public IReadOnlyList<(decimal Price, decimal Size)> Asks => _asks;

        public decimal MidPrice { get; private set; }

        public BfOrderBookSnapshot(
            IEnumerable<KeyValuePair<decimal, decimal>> bids,
            IEnumerable<KeyValuePair<decimal, decimal>> asks,
            decimal midPrice,
            int size
        )
        {
            asks.Take(size).ForEach(e => _asks.Add((e.Key, e.Value)));
            bids.TakeLast(size).ForEach(e => _bids.Add((e.Key, e.Value)));
            MidPrice = midPrice;
        }
    }
}
