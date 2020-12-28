//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi
{
    public interface IBfPrivateExecution : IBfExecution
    {
        string ChildOrderId { get; }
        decimal? Commission { get; }
    }
}
