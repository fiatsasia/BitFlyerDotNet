//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Reactive.Disposables;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    internal static class RxUtil
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
        public static BfParentOrderRequestParameter ToParameter(this BfChildOrderRequest child)
        {
            return new BfParentOrderRequestParameter
            {
                ProductCode = child.ProductCode,
                ConditionType = child.ChildOrderType,
                Side = child.Side,
                Size = child.Size,
                Price = child.Price,
            };
        }

        public static BfParentOrderRequestParameter ToParameter(this BfParentOrderRequest parent)
        {
            if (parent.Parameters.Count != 1)
            {
                throw new ArgumentException();
            }
            return parent.Parameters[0];
        }
    }
}
