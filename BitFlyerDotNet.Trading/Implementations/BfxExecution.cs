//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    class BfxExecution
    {
        public long Id { get; }
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
