//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    class BfxExecution : IBfxExecution
    {
        public int Id { get; }
        public DateTime Time { get; }
        public decimal Price { get; }
        public decimal Size { get; }
        public decimal? Commission { get; }
        public decimal? SfdCollectedAmount { get; }
        public string OrderId { get; }

        public BfxExecution(BfChildOrderEvent coe)
        {
            Id = coe.ExecutionId;
            Time = coe.EventDate;
            Price = coe.Price;
            Size = coe.Size;
            Commission = coe.Commission;
            SfdCollectedAmount = coe.SwapForDifference;
            OrderId = coe.ChildOrderId;
        }

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
}
