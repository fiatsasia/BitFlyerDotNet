﻿//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public interface IBfOrder
{
}

public static class IBfOrderExtensions
{
    public static string GetProductCode(this IBfOrder order) => order switch
    {
        BfParentOrder parentOrder => parentOrder.Parameters[0].ProductCode,
        BfChildOrder childOrder => childOrder.ProductCode,
        _ => throw new ArgumentException()
    };

    public static BfOrderType GetOrderType(this IBfOrder order) => order switch
    {
        BfParentOrder parentOrder => parentOrder.OrderMethod != BfOrderType.Simple ? parentOrder.OrderMethod : parentOrder.Parameters[0].ConditionType,
        BfChildOrder childOrder => childOrder.ChildOrderType,
        _ => throw new ArgumentException()
    };
}