//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System.Linq;
using System.Collections.Generic;

namespace BitFlyerDotNet.LightningApi
{
    public class BfOrderBookSnapshot
    {
        List<KeyValuePair<decimal, decimal>> _bids = new List<KeyValuePair<decimal, decimal>>();
        public IReadOnlyList<(decimal Price, decimal Size)> Bids => _bids.Select(e => (Price: e.Key, Size: e.Value)).ToList();

        List<KeyValuePair<decimal, decimal>> _asks = new List<KeyValuePair<decimal, decimal>>();
        public IReadOnlyList<(decimal Price, decimal Size)> Asks => _asks.Select(e => (Price: e.Key, Size: e.Value)).ToList();

        public decimal MidPrice { get; private set; }

        public BfOrderBookSnapshot(
            IEnumerable<KeyValuePair<decimal, decimal>> bids,
            IEnumerable<KeyValuePair<decimal, decimal>> asks,
            decimal midPrice,
            int size
        )
        {
            _asks.AddRange(asks.Take(size));
            _bids.AddRange(bids.TakeLast(size));
            MidPrice = midPrice;
        }
    }
}
