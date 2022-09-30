//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.DataSource;

public class BfPositionContext
{
    public string ChildOrderAcceptanceId { get; }
    public int ExecutionIndex { get; }
    public DateTime Time { get; private set; }
    public decimal Price { get; private set; }
    public decimal OpenSize { get; private set; }
    public BfTradeSide Side => OpenSize > decimal.Zero ? BfTradeSide.Buy : BfTradeSide.Sell;

    public decimal CurrentSize { get; private set; }
    public decimal SwapPointAccumulate { get; }

    decimal _commission;
    public decimal Commission => _commission * (CurrentSize / OpenSize);
    decimal _sfd;
    public decimal SwapForDifference => _sfd * (CurrentSize / OpenSize);

    private BfPositionContext()
    {
        ChildOrderAcceptanceId = string.Empty;
    }

    public BfPositionContext(BfPosition pos)
    {
        ChildOrderAcceptanceId = string.Empty;
        Time = pos.OpenDate;
        Price = pos.Price;
        CurrentSize = OpenSize = pos.Side == BfTradeSide.Buy ? pos.Size : -pos.Size;
        SwapPointAccumulate = pos.SwapPointAccumulate;
        _commission = pos.Commission;
        _sfd = pos.SwapForDifference;
    }

    public BfPositionContext(BfChildOrderEvent e, decimal size)
    {
        if (e.EventType != BfOrderEventType.Execution)
        {
            throw new ArgumentException();
        }

        ChildOrderAcceptanceId = e.ChildOrderAcceptanceId;
        Time = e.EventDate;
        CurrentSize = OpenSize = e.Side == BfTradeSide.Buy ? size : -size;
#pragma warning disable CS8629
        Price = e.Price.Value;
        _commission = e.Commission.Value;
        _sfd = e.SwapForDifference.Value;
#pragma warning restore CS8629
    }

    public BfPositionContext Split(decimal splitSize)
    {
        var newPos = new BfPositionContext
        {
            Time = this.Time,
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
