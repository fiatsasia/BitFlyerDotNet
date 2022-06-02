﻿//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Collections.Generic;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxPosition
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
        public BfxPositionManager Positions { get; }

        internal BfxPosition(BfxPositionManager positions, BfxPositionsElement pos, BfChildOrderEvent? ev = default)
        {
            Positions = positions;
            Open = pos.Open;
            Close = ev?.EventDate;
            Side = pos.OpenSize > 0m ? BfTradeSide.Buy : BfTradeSide.Sell;
            OpenPrice = pos.Price;
            ClosePrice = ev?.Price;
            Size = Math.Abs(pos.CurrentSize);
            Commission = pos.Commission;
            SwapForDifference = pos.SwapForDifference;
            SwapPointAccumulate = pos.SwapPointAccumulate;
        }

        public decimal? Profit => ClosePrice.HasValue ? Math.Floor((ClosePrice.Value - OpenPrice) * (Side == BfTradeSide.Buy ? Size : -Size)) : default;
        public bool IsOpened => !Close.HasValue;
        public bool IsClosed => Close.HasValue;
    }

    class BfxPositionsElement
    {
        public string ChildOrderAcceptanceId { get; }
        public int ExecutionIndex { get; }
        public DateTime Open { get; private set; }
        public decimal Price { get; private set; }
        public decimal OpenSize { get; private set; }
        public BfTradeSide Side => OpenSize > decimal.Zero ? BfTradeSide.Buy : BfTradeSide.Sell;

        public decimal CurrentSize { get; private set; }
        public decimal SwapPointAccumulate { get; }

        decimal _commission;
        public decimal Commission => _commission * (CurrentSize / OpenSize);
        decimal _sfd;
        public decimal SwapForDifference => _sfd * (CurrentSize / OpenSize);

        private BfxPositionsElement()
        {
            ChildOrderAcceptanceId = string.Empty;
        }

        public BfxPositionsElement(BfPosition pos)
        {
            ChildOrderAcceptanceId = string.Empty;
            Open = pos.OpenDate;
            Price = pos.Price;
            CurrentSize = OpenSize = pos.Side == BfTradeSide.Buy ? pos.Size : -pos.Size;
            SwapPointAccumulate = pos.SwapPointAccumulate;
            _commission = pos.Commission;
            _sfd = pos.SwapForDifference;
        }

#pragma warning disable CS8629
        public BfxPositionsElement(BfChildOrderEvent e, decimal size)
        {
            if (e.EventType != BfOrderEventType.Execution)
            {
                throw new ArgumentException();
            }

            ChildOrderAcceptanceId = e.ChildOrderAcceptanceId;
            Open = e.EventDate;
            Price = e.Price.Value;
            CurrentSize = OpenSize = e.Side == BfTradeSide.Buy ? size : -size;
            _commission = e.Commission.Value;
            _sfd = e.SwapForDifference.Value;
        }
#pragma warning restore CS8629

        internal BfxPositionsElement Split(decimal splitSize)
        {
            var newPos = new BfxPositionsElement
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

    public class BfxPositionManager
    {
        public event EventHandler<BfxPositionChangedEventArgs>? PositionChanged;

        Queue<BfxPositionsElement> _q = new Queue<BfxPositionsElement>();

        public decimal TotalSize => Math.Abs(_q.Sum(e => e.CurrentSize));
        public BfTradeSide Side => _q.Count == 0 ? BfTradeSide.Unknown : _q.Peek().Side;

        public IEnumerable<BfxPosition> GetActivePositions()
        {
            return _q.ToList().Select(e => new BfxPosition(this, e));
        }

        public void Update(BfPrivateExecution[] execs)
        {
            throw new NotImplementedException();
        }

        public void Update(BfPosition[] positions)
        {
            _q.Clear();
            positions.ForEach(e => _q.Enqueue(new BfxPositionsElement(e)));
        }

#pragma warning disable CS8629
        public BfxPosition[] Update(BfChildOrderEvent e)
        {
            if (e.EventType != BfOrderEventType.Execution)
            {
                throw new ArgumentException();
            }

            var executedSize = e.Side == BfTradeSide.Buy ? e.Size.Value : -e.Size.Value;
            if (_q.Count == 0 || Math.Sign(_q.Peek().OpenSize) == Math.Sign(executedSize))
            {
                var pos = new BfxPositionsElement(e, e.Size.Value);
                _q.Enqueue(pos);
                return new BfxPosition[] { new BfxPosition(this, pos) };
            }

            // Process to another side
            var closeSize = executedSize;
            var closedPos = new List<BfxPositionsElement>();
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
            var result = new List<BfxPosition>();
            closedPos.ForEach(pos => result.Add(new BfxPosition(this, pos, e)));

            if (closeSize > 0m)
            {
                var pos = new BfxPositionsElement(e, Math.Abs(closeSize));
                _q.Enqueue(pos);
                result.Add(new BfxPosition(this, pos));
            }

            return result.ToArray();
        }
#pragma warning restore CS8629
    }
}
