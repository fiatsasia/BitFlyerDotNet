//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

class BfxPositionManager
{
    Queue<BfxActivePosition> _q = new Queue<BfxActivePosition>();

    public decimal TotalSize => Math.Abs(_q.Sum(e => e.CurrentSize));
    public BfTradeSide Side => _q.Count == 0 ? BfTradeSide.Unknown : _q.Peek().Side;

    public IEnumerable<BfxPosition> GetActivePositions()
    {
        return _q.ToList().Select(e => new BfxPosition(e));
    }

    public void Update(BfPosition[] positions)
    {
        _q.Clear();
        positions.ForEach(e => _q.Enqueue(new BfxActivePosition(e)));
    }

    public IEnumerable<BfxPosition> Update(BfChildOrderEvent e)
    {
        if (e.EventType != BfOrderEventType.Execution)
        {
            throw new ArgumentException();
        }

#pragma warning disable CS8629
        var executedSize = e.Side == BfTradeSide.Buy ? e.Size.Value : -e.Size.Value;
#pragma warning restore CS8629
        if (_q.Count == 0 || Math.Sign(_q.Peek().OpenSize) == Math.Sign(executedSize)) // empty or same side
        {
            var pos = new BfxActivePosition(e, e.Size.Value);
            _q.Enqueue(pos);
            return new BfxPosition[] { new BfxPosition(pos) };
        }

        // Process to another side
        var closeSize = executedSize;
        var closedPos = new List<BfxActivePosition>();
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
        closedPos.ForEach(pos => result.Add(new BfxPosition(pos, e)));

        if (closeSize > 0m)
        {
            var pos = new BfxActivePosition(e, Math.Abs(closeSize));
            _q.Enqueue(pos);
            result.Add(new BfxPosition(pos));
        }

        return result;
    }
}
