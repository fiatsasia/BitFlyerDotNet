﻿//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Reactive.Disposables;

namespace BitFlyerDotNet.Historical
{
    static class EnumUtil
    {
        public static string ToEnumString<TEnum>(this TEnum type) where TEnum : struct
        {
            var enumType = typeof(TEnum);
            var name = Enum.GetName(enumType, type);
            var enumMemberAttribute = ((EnumMemberAttribute[])enumType.GetField(name).GetCustomAttributes(typeof(EnumMemberAttribute), true)).SingleOrDefault();
            return enumMemberAttribute?.Value ?? type.ToString();
        }
    }

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
