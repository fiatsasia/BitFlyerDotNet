//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Reactive.Disposables;

namespace BitFlyerDotNet.Historical
{
    static class MathEx
    {
        public static decimal Truncate(this decimal value, int precision)
        {
            var step = (decimal)Math.Pow(10, precision);
            var trunc = Math.Truncate(step * value);
            return trunc / step;
        }

        public static decimal Ceiling(this decimal value, int precision)
        {
            var step = (decimal)Math.Pow(10, precision);
            var ceil = Math.Ceiling(step * value);
            return ceil / step;
        }
    }

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

    public static class DateTimeExtensions
    {
        public static DateTime Round(this DateTime dt, TimeSpan unit)
        {
            return new DateTime(dt.Ticks / unit.Ticks * unit.Ticks, dt.Kind);
        }
    }
}
