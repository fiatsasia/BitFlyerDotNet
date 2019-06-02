//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System.Linq;
using System.Collections.Generic;

namespace BitFlyerDotNet.LightningApi
{
    public class BfOrderBook
    {
        SortedDictionary<decimal, decimal> _bids { get; } = new SortedDictionary<decimal, decimal>();
        SortedDictionary<decimal, decimal> _asks { get; } = new SortedDictionary<decimal, decimal>();
        KeyValuePair<decimal, decimal> _bestBid;
        KeyValuePair<decimal, decimal> _bestAsk;

        public decimal MidPrice { get; private set; }
        public decimal BestBidPrice => _bestBid.Key;
        public decimal BestBidSize => _bestBid.Value;
        public decimal BestAskPrice => _bestAsk.Key;
        public decimal BestAskSize => _bestAsk.Value;

        static readonly KeyValuePair<decimal, decimal> DefaultElement = new KeyValuePair<decimal, decimal>(decimal.Zero, decimal.Zero);

        public void Reset(BfBoard orders)
        {
            MidPrice = orders.MidPrice;
            _bids.Clear(); orders.Bids.ForEach(e => _bids.Add(e.Price, e.Size));
            _bestBid = _bids.Last();

            _asks.Clear(); orders.Asks.ForEach(e => _asks.Add(e.Price, e.Size));
            _bestAsk = _asks.First();
        }

        public void UpdateDelta(BfBoard orders)
        {
            MidPrice = orders.MidPrice;

            foreach (var bid in orders.Bids)
            {
                if (bid.Size == decimal.Zero)
                {
                    _bids.Remove(bid.Price);
                }
                else
                {
                    _bids[bid.Price] = bid.Size;
                }
            }
            _bestBid = _bids.Count() > 0 ? _bids.Last() : DefaultElement;

            foreach (var ask in orders.Asks)
            {
                if (ask.Size == decimal.Zero)
                {
                    _asks.Remove(ask.Price);
                }
                else
                {
                    _asks[ask.Price] = ask.Size;
                }
            }
            _bestAsk = _asks.Count() > 0 ? _asks.First() : DefaultElement;
        }

        public BfOrderBookSnapshot GetSnapshot(int size)
        {
            return new BfOrderBookSnapshot(_bids, _asks, MidPrice, size);
        }
    }
}
