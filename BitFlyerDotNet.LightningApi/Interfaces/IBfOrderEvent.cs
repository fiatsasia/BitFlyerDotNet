//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public interface IBfOrderEvent
{
}

public static class IBfOrderEventExtensions
{
    public static string GetAcceptanceId(this IBfOrderEvent e) => e switch
    {
        BfChildOrderEvent coe => coe.ChildOrderAcceptanceId,
        BfParentOrderEvent poe => poe.ParentOrderAcceptanceId,
        _ => throw new ArgumentException()
    };
}
