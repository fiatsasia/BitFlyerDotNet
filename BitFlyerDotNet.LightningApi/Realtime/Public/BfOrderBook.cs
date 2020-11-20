//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System.Linq;
using System.Collections.Generic;

namespace BitFlyerDotNet.LightningApi
{
    public class BfOrderBook
    {
        SortedDictionary<decimal, decimal> _bids { get; } = new ();
        SortedDictionary<decimal, decimal> _asks { get; } = new ();
        KeyValuePair<decimal, decimal> _bestBid;
        KeyValuePair<decimal, decimal> _bestAsk;

        public double MidPrice { get; private set; }
        public decimal BestBidPrice => _bestBid.Key;
        public decimal BestBidSize => _bestBid.Value;
        public decimal BestAskPrice => _bestAsk.Key;
        public decimal BestAskSize => _bestAsk.Value;

        public double TotalBidDepth { get; private set; }
        public double TotalAskDepth { get; private set; }

        static readonly KeyValuePair<decimal, decimal> DefaultElement = new (decimal.Zero, decimal.Zero);

        object _lockObject = new ();

        public void Reset(BfBoard orders)
        {
            lock (_lockObject)
            {
                MidPrice = unchecked((double)orders.MidPrice);

                TotalBidDepth = 0;
                _bids.Clear(); orders.Bids.ForEach(e =>
                {
                    _bids.Add(e.Price, e.Size);
                    TotalBidDepth += unchecked((double)e.Size);
                });
                _bestBid = _bids.Last();

                TotalAskDepth = 0;
                _asks.Clear(); orders.Asks.ForEach(e =>
                {
                    _asks.Add(e.Price, e.Size);
                    TotalAskDepth += unchecked((double)e.Size);
                });
                _bestAsk = _asks.First();
            }
        }

        public void UpdateDelta(BfBoard orders)
        {
            lock (_lockObject)
            {
                MidPrice = unchecked((double)orders.MidPrice);

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
                    TotalBidDepth = unchecked((double)_bids.Values.Sum());
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
                    TotalAskDepth = unchecked((double)_asks.Values.Sum());
                }
            }
        }

        public BfOrderBookSnapshot GetSnapshot(int size)
        {
            lock (_lockObject)
            {
                return new (_bids, _asks, unchecked((decimal)MidPrice), size);
            }
        }
    }
}
