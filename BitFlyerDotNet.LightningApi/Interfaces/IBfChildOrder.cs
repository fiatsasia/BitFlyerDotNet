//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
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
