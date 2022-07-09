//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

public class BfxPosition
{
    public DateTime OpenTime { get; }
    public DateTime? CloseTime { get; }
    public BfTradeSide Side { get; }
    public decimal OpenPrice { get; }
    public decimal? ClosePrice { get; }
    public decimal Size { get; }
    public decimal Commission { get; }
    public decimal SwapForDifference { get; }
    public decimal SwapPointAccumulate { get; }

    internal BfxPosition(BfxActivePosition pos, BfChildOrderEvent? ev = default)
    {
        OpenTime = pos.Time;
        CloseTime = ev?.EventDate;
        Side = pos.OpenSize > 0m ? BfTradeSide.Buy : BfTradeSide.Sell;
        OpenPrice = pos.Price;
        ClosePrice = ev?.Price;
        Size = Math.Abs(pos.CurrentSize);
        Commission = pos.Commission;
        SwapForDifference = pos.SwapForDifference;
        SwapPointAccumulate = pos.SwapPointAccumulate;
    }

    public decimal? Profit => ClosePrice.HasValue ? Math.Floor((ClosePrice.Value - OpenPrice) * (Side == BfTradeSide.Buy ? Size : -Size)) : default;
    public bool IsOpened => !CloseTime.HasValue;
    public bool IsClosed => CloseTime.HasValue;
}
