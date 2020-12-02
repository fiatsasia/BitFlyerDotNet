//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet
{
    public class BfExecution : IBfPrivateExecution
    {
        public int ExecutionId { get; }
        public BfTradeSide Side { get; }
        public decimal Price { get; }
        public decimal Size { get; }
        public DateTime ExecutedTime { get; }
        public string ChildOrderAcceptanceId { get; }
        public string ChildOrderId { get; }
        public decimal? Commission { get; }

        public BfExecution(BfaPrivateExecution exec)
        {
            ExecutionId = exec.ExecutionId;
            Side = exec.Side;
            Price = exec.Price;
            Size = exec.Size;
            ExecutedTime = exec.ExecutedTime;
            ChildOrderAcceptanceId = exec.ChildOrderAcceptanceId;
            ChildOrderId = exec.ChildOrderId;
            Commission = exec.Commission;
        }
    }
}
