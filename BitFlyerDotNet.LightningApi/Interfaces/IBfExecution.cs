//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;

namespace BitFlyerDotNet.LightningApi
{
    // Common interface between BfExecution and BfPrivateExecution
    public interface IBfExecution
    {
        int ExecutionId { get; }
        BfTradeSide Side { get; }
        decimal Price { get; }
        decimal Size { get; }
        DateTime ExecutedTime { get; }
        string ChildOrderAcceptanceId { get; }
    }
}
