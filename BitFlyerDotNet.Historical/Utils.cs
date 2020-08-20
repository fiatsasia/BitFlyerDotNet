//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Reactive.Disposables;

namespace BitFlyerDotNet.Historical
{
    static class RxUtil
    {
        public static TResult AddTo<TResult>(this TResult resource, CompositeDisposable disposable) where TResult : IDisposable
        {
            disposable.Add(resource);
            return resource;
        }
    }

    static class DateTimeExtensions
    {
        public static DateTime Round(this DateTime dt, TimeSpan unit)
        {
            return new DateTime(dt.Ticks / unit.Ticks * unit.Ticks, dt.Kind);
        }
    }
}
