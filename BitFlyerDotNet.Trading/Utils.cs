//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

internal static class RxExtensions
{
    public static TResult AddTo<TResult>(this TResult resource, CompositeDisposable disposable) where TResult : IDisposable
    {
        disposable.Add(resource);
        return resource;
    }

    public static void DisposeReverse(this CompositeDisposable disposable)
    {
        disposable.Reverse().ForEach(e => e.Dispose());
    }
}

internal static class BfOrderConvertExtension
{
    public static BfParentOrderParameter ToParameter(this BfChildOrder child)
    {
        return new BfParentOrderParameter
        {
            ProductCode = child.ProductCode,
            ConditionType = child.ChildOrderType,
            Side = child.Side,
            Size = child.Size,
            Price = child.Price,
        };
    }
}
