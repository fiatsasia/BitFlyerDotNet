//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxPositionChange
    {
        public DateTime Open { get; }
        public DateTime? Close { get; }
        public BfTradeSide Side { get; }
        public decimal OpenPrice { get; }
        public decimal? ClosePrice { get; }
        public decimal Size { get; }
        public decimal Commission { get; }
        public decimal SwapForDifference { get; }
        public decimal SwapPointAccumulate { get; }

        internal BfxPositionChange(BfxPosition pos, BfChildOrderEvent? evt = default)
        {
            Open = pos.Open;
            Close = evt?.EventDate;
            Side = pos.OpenSize > 0m ? BfTradeSide.Buy : BfTradeSide.Sell;
            OpenPrice = pos.Price;
            ClosePrice = evt?.Price;
            Size = Math.Abs(pos.CurrentSize);
            Commission = pos.Commission;
            SwapForDifference = pos.SwapForDifference;
            SwapPointAccumulate = pos.SwapPointAccumulate;
        }

        public decimal? Profit => (ClosePrice - OpenPrice) * (Side == BfTradeSide.Buy ? Size : -Size);
        public decimal? NetProfit => Profit - Commission - SwapForDifference - SwapPointAccumulate;
    }

    class BfxPosition
    {
        public DateTime Open { get; private set; }
        public decimal Price { get; private set; }
        public decimal OpenSize { get; private set; }

        public decimal CurrentSize { get; private set; }
        public decimal SwapPointAccumulate { get; }

        decimal _commission;
        public decimal Commission => _commission * (CurrentSize / OpenSize);
        decimal _sfd;
        public decimal SwapForDifference => _sfd * (CurrentSize / OpenSize);

        private BfxPosition() { }

        public BfxPosition(BfPosition pos)
        {
            Open = pos.OpenDate;
            Price = pos.Price;
            CurrentSize = OpenSize = pos.Side == BfTradeSide.Buy ? pos.Size : -pos.Size;
            SwapPointAccumulate = pos.SwapPointAccumulate;
            _commission = pos.Commission;
            _sfd = pos.SwapForDifference;
        }

        public BfxPosition(BfChildOrderEvent evt, decimal size)
        {
            Open = evt.EventDate;
            Price = evt.Price;
            CurrentSize = OpenSize = evt.Side == BfTradeSide.Buy ? size : -size;
            _commission = evt.Commission;
            _sfd = evt.SwapForDifference;
        }

        public BfxPosition(BfChildOrderEvent evt)
            : this(evt, evt.Size)
        {
        }

        internal BfxPosition Split(decimal splitSize)
        {
            var newPos = new BfxPosition
            {
                Open = this.Open,
                Price = this.Price,
                OpenSize = this.OpenSize,
                CurrentSize = -splitSize,
                _commission = this._commission,
                _sfd = this._sfd,
            };
            CurrentSize += splitSize;
            return newPos;
        }
    }

    public class BfxPositions
    {
        Queue<BfxPosition> _q = new Queue<BfxPosition>();

        public void Update(BfPosition[] positions)
        {
            positions.ForEach(e => _q.Enqueue(new BfxPosition(e)));
        }

        public BfxPositionChange[] Update(BfChildOrderEvent evt)
        {
            var executedSize = evt.Side == BfTradeSide.Buy ? evt.Size : -evt.Size;
            if (_q.Count == 0 || Math.Sign(_q.Peek().OpenSize) == Math.Sign(executedSize))
            {
                var pos = new BfxPosition(evt);
                _q.Enqueue(pos);
                return new BfxPositionChange[] { new BfxPositionChange(pos) };
            }

            // Process to another side
            var closeSize = executedSize;
            var closedPos = new List<BfxPosition>();
            while (Math.Abs(closeSize) > 0m && _q.Count > 0)
            {
                var pos = _q.Peek();
                if (Math.Abs(closeSize) >= Math.Abs(pos.CurrentSize))
                {
                    closeSize += pos.CurrentSize;
                    closedPos.Add(_q.Dequeue());
                    continue;
                }
                if (Math.Abs(closeSize) < Math.Abs(pos.CurrentSize))
                {
                    closedPos.Add(pos.Split(closeSize));
                    closeSize = 0;
                    break;
                }
            }
            var result = new List<BfxPositionChange>();
            closedPos.ForEach(e => result.Add(new BfxPositionChange(e, evt)));

            if (closeSize > 0m)
            {
                var pos = new BfxPosition(evt, Math.Abs(closeSize));
                _q.Enqueue(pos);
                result.Add(new BfxPositionChange(pos));
            }

            return result.ToArray();
        }
    }
}
