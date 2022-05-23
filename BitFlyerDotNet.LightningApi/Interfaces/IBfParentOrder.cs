//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;

namespace BitFlyerDotNet.LightningApi
{
    public interface IBfParentOrder
    {
        string ProductCode { get; }
        BfOrderType OrderType { get; }
        string AcceptanceId { get; }
        string OrderId { get; }
        DateTime OrderDate { get; }
        DateTime ExpireDate { get; }
        BfOrderState State { get; }
        IBfChildOrder[] Children { get; }
    }
}
