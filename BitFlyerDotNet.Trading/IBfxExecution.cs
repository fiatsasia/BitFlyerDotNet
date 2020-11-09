//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;

namespace BitFlyerDotNet.Trading
{
    public interface IBfxExecution
    {
        int Id { get; }
        DateTime Time { get; }
        decimal Price { get; }
        decimal Size { get; }
        decimal? Commission { get; }
        decimal? SfdCollectedAmount { get; }
        string OrderId { get; }
    }
}
