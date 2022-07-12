﻿//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

public class BfxExecution
{
    public long Id { get; private set; }
    public DateTime Time { get; private set; }
    public BfTradeSide Side { get; private set; }
    public decimal Price { get; private set; }
    public decimal Size { get; private set; }
    public decimal Commission { get; private set; }
    public decimal? SwapForDifference { get; private set; }
    public string OrderId { get; private set; }
    public string OrderAcceptanceId { get; private set; }

#pragma warning disable CS8629
    public BfxExecution(BfChildOrderEvent e)
    {
        if (e.EventType != BfOrderEventType.Execution) throw new ArgumentException();
        Id = e.ExecutionId.Value;
        Time = e.EventDate;
        Side = e.Side.Value;
        Price = e.Price.Value;
        Size = e.Size.Value;
        Commission = e.Commission.Value;
        SwapForDifference = e.SwapForDifference;
        OrderId = e.ChildOrderId;
        OrderAcceptanceId = e.ChildOrderAcceptanceId;
    }
#pragma warning restore CS8629

    public BfxExecution(BfPrivateExecution exec)
    {
        Id = exec.ExecutionId;
        Time = exec.ExecutedTime;
        Side = exec.Side;
        Price = exec.Price;
        Size = exec.Size;
        Commission = exec.Commission;
        OrderId = exec.ChildOrderId;
        OrderAcceptanceId = exec.ChildOrderAcceptanceId;
    }
}