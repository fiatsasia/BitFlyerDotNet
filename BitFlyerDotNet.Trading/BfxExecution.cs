//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxExecution : IBfxExecution
    {
        public int Id { get; }
        public DateTime Time { get; }
        public decimal Price { get; }
        public decimal Size { get; }
        public decimal? Commission { get; }
        public decimal? SfdCollectedAmount { get; }

        public BfxExecution(BfChildOrderEvent coe)
        {
            Id = coe.ExecutionId;
            Time = coe.EventDate;
            Price = coe.Price;
            Size = coe.Size;
            Commission = coe.Commission;
            SfdCollectedAmount = coe.SfdCollectedAmount;
        }

        public BfxExecution(BfPrivateExecution exec)
        {
            Id = exec.ExecutionId;
            Time = exec.ExecutedTime;
            Price = exec.Price;
            Size = exec.Size;
            Commission = exec.Commission;
        }
    }
}
