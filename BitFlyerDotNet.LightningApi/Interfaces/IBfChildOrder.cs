//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet
{
    public interface IBfChildOrder
    {
        BfProductCode ProductCode { get; }
        BfOrderType OrderType { get; }
        BfTradeSide Side { get; }
        decimal? OrderPrice { get; }
        decimal OrderSize { get; }
        DateTime? OrderDate { get; }
        DateTime? ExpireDate { get; }
        BfOrderState State { get; }

        string AcceptanceId { get; }
        string OrderId { get; }
        IBfPrivateExecution[] Executions { get; set; }
    }
}
