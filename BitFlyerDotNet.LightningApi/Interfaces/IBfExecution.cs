//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;

namespace BitFlyerDotNet.LightningApi
{
    // Common interface between BfExecution and BfPrivateExecution
    public interface IBfExecution
    {
        long ExecutionId { get; }
        BfTradeSide Side { get; }
        decimal Price { get; }
        decimal Size { get; }
        DateTime ExecutedTime { get; }
        string ChildOrderAcceptanceId { get; }
    }
}
