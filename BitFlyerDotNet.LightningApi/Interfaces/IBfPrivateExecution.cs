//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;

namespace BitFlyerDotNet.LightningApi
{
    public interface IBfPrivateExecution : IBfExecution
    {
        string ChildOrderId { get; }
        decimal? Commission { get; }
    }
}
