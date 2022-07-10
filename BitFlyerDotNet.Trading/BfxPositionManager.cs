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
    ConcurrentQueue<BfxActivePosition> _q = new ConcurrentQueue<BfxActivePosition>();

    public decimal TotalSize => Math.Abs(_q.Sum(e => e.CurrentSize));
    public BfTradeSide Side => _q.TryPeek(out var pos) ? pos.Side : BfTradeSide.Unknown;

    public BfxPositionManager() { }
    public BfxPositionManager(BfPosition[] positions)
    {
        positions.ForEach(e => _q.Enqueue(new BfxActivePosition(e)));
    }

    public IEnumerable<BfxPosition> GetActivePositions()
    {
        return _q.ToList().Select(e => new BfxPosition(e));
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
        BfxActivePosition pos;
        if (!_q.TryPeek(out pos) || Math.Sign(pos.OpenSize) == Math.Sign(executedSize)) // empty or same side
        {
            pos = new BfxActivePosition(e, e.Size.Value);
            _q.Enqueue(pos);
            return new BfxPosition[] { new BfxPosition(pos) };
        }

        // Process to another side
        var closeSize = executedSize;
        var closedPos = new List<BfxActivePosition>();
        while (_q.TryPeek(out pos) && Math.Abs(closeSize) > 0m)
        {
            if (Math.Abs(closeSize) >= Math.Abs(pos.CurrentSize))
            {
                closeSize += pos.CurrentSize;
                if (_q.TryDequeue(out pos))
                {
                    closedPos.Add(pos);
                }
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
            pos = new BfxActivePosition(e, Math.Abs(closeSize));
            _q.Enqueue(pos);
            result.Add(new BfxPosition(pos));
        }

        return result;
    }
}
