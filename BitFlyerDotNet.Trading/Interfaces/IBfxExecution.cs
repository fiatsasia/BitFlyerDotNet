//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;

namespace BitFlyerDotNet.Trading
{
    public interface IBfxExecution
    {
        long Id { get; }
        DateTime Time { get; }
        decimal Price { get; }
        decimal Size { get; }
        decimal? Commission { get; }
        decimal? SfdCollectedAmount { get; }
        string OrderId { get; }
    }
}
