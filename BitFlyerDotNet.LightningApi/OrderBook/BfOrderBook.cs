//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System.Linq;
using System.Collections.Generic;
using Financial.Extensions;

namespace BitFlyerDotNet.LightningApi
{
    public class BfOrderBook : IFxOrderBook
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

        public decimal TotalBidDepth { get; private set; }
        public decimal TotalAskDepth { get; private set; }

        static readonly KeyValuePair<decimal, decimal> DefaultElement = new KeyValuePair<decimal, decimal>(decimal.Zero, decimal.Zero);

        public void Reset(BfBoard orders)
        {
            MidPrice = orders.MidPrice;

            TotalBidDepth = 0;
            _bids.Clear(); orders.Bids.ForEach(e =>
            {
                _bids.Add(e.Price, e.Size);
                TotalBidDepth += e.Size;
            });
            _bestBid = _bids.Last();

            TotalAskDepth = 0;
            _asks.Clear(); orders.Asks.ForEach(e =>
            {
                _asks.Add(e.Price, e.Size);
                TotalAskDepth += e.Size;
            });
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

            if (_bids.Count() == 0)
            {
                _bestBid = DefaultElement;
                TotalBidDepth = 0;
            }
            else
            {
                _bestBid = _bids.Last();
                TotalBidDepth = _bids.Values.Sum();
            }

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

            if (_asks.Count() == 0)
            {
                _bestAsk = DefaultElement;
                TotalAskDepth = 0;
            }
            else
            {
                _bestAsk = _asks.First();
                TotalAskDepth = _asks.Values.Sum();
            }
        }

        public BfOrderBookSnapshot GetSnapshot(int size)
        {
            return new BfOrderBookSnapshot(_bids, _asks, MidPrice, size);
        }
    }
}
