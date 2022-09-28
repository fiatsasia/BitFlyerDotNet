//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.DataSource;

class BdPositionManager
{
    public string ProductCode { get; private set; }
    public decimal TotalSize => Math.Abs(_q.Sum(e => e.CurrentSize));

    ConcurrentQueue<BdPositionContext> _q = new ConcurrentQueue<BdPositionContext>();

    public BdPositionManager(BfPosition[] positions)
    {
        ProductCode = BfProductCode.FX_BTC_JPY;
        positions.ForEach(e => _q.Enqueue(new BdPositionContext(e)));
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

        var executedSize = e.Side == BfTradeSide.Buy ? e.Size.Value : -e.Size.Value;
        BdPositionContext ctx;
        if (!_q.TryPeek(out ctx) || Math.Sign(ctx.OpenSize) == Math.Sign(executedSize)) // empty or same side
        {
            ctx = new BdPositionContext(e, e.Size.Value);
            _q.Enqueue(ctx);
            return new BfxPosition[] { new BfxPosition(ctx) };
        }

        // Process to another side
        var closeSize = executedSize;
        var closedPos = new List<BdPositionContext>();
        while (_q.TryPeek(out ctx) && Math.Abs(closeSize) > 0m)
        {
            if (Math.Abs(closeSize) >= Math.Abs(ctx.CurrentSize))
            {
                closeSize += ctx.CurrentSize;
                if (_q.TryDequeue(out ctx))
                {
                    closedPos.Add(ctx);
                }
                continue;
            }
            if (Math.Abs(closeSize) < Math.Abs(ctx.CurrentSize))
            {
                closedPos.Add(ctx.Split(closeSize));
                closeSize = 0;
                break;
            }
        }
        var result = new List<BfxPosition>();
        closedPos.ForEach(pos => result.Add(new BfxPosition(pos, e)));

        if (closeSize > 0m)
        {
            ctx = new BdPositionContext(e, Math.Abs(closeSize));
            _q.Enqueue(ctx);
            result.Add(new BfxPosition(ctx));
        }

        return result;
    }
}
