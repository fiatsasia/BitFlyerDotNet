//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;

namespace BitFlyerDotNet.LightningApi
{
    public interface IBfParentOrder
    {
        BfProductCode ProductCode { get; }
        BfOrderType OrderType { get; }
        string AcceptanceId { get; }
        string OrderId { get; }
        DateTime OrderDate { get; }
        DateTime ExpireDate { get; }
        BfOrderState State { get; }
        int MinuteToExpire { get; }
        IBfChildOrder[] Children { get; }
    }
}
