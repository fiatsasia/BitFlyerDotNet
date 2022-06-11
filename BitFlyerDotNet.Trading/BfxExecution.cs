//==============================================================================
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
    public decimal Price { get; private set; }
    public decimal Size { get; private set; }
    public decimal Commission { get; private set; }
    public decimal? SwapForDifference { get; private set; }
    public string OrderId { get; private set; }

#pragma warning disable CS8629
    public BfxExecution(BfChildOrderEvent e)
    {
        OrderId = e.ChildOrderId;
        Id = e.ExecutionId.Value;
        Update(e);
    }

    public BfxExecution Update(BfChildOrderEvent e)
    {
        if (e.EventType != BfOrderEventType.Execution) throw new ArgumentException();
        Time = e.EventDate;
        Price = e.Price.Value;
        Size = e.Size.Value;
        Commission = e.Commission.Value;
        SwapForDifference = e.SwapForDifference;
        return this;
    }
#pragma warning restore CS8629

    public BfxExecution(BfPrivateExecution exec)
    {
        Id = exec.ExecutionId;
        Time = exec.ExecutedTime;
        Price = exec.Price;
        Size = exec.Size;
        Commission = exec.Commission;
        OrderId = exec.ChildOrderId;
    }
}
