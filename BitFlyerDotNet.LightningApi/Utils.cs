//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Reactive.Disposables;
using Newtonsoft.Json;

namespace BitFlyerDotNet.LightningApi
{
    internal static class EnumUtil
    {
        internal static string ToEnumString<TEnum>(this TEnum type) where TEnum : struct
        {
            var enumType = typeof(TEnum);
            var name = Enum.GetName(enumType, type);
            var enumMemberAttribute = ((EnumMemberAttribute[])enumType.GetField(name).GetCustomAttributes(typeof(EnumMemberAttribute), true)).SingleOrDefault();
            return enumMemberAttribute?.Value ?? type.ToString();
        }
    }

    internal static class RxUtil
    {
        public static TResult AddTo<TResult>(this TResult resource, CompositeDisposable disposable) where TResult : IDisposable
        {
            disposable.Add(resource);
            return resource;
        }
    }

    class DecimalJsonConverter : JsonConverter
    {
        public DecimalJsonConverter() { }
        public override bool CanRead { get { return false; } }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(decimal) || objectType == typeof(float) || objectType == typeof(double));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (DecimalJsonConverter.IsWholeValue(value))
            {
                writer.WriteRawValue(JsonConvert.ToString(Convert.ToInt64(value)));
            }
            else
            {
                writer.WriteRawValue(JsonConvert.ToString(value));
            }
        }

        private static bool IsWholeValue(object value)
        {
            switch (value)
            {
                case decimal dec:
                    int precision = (Decimal.GetBits((decimal)(double)dec)[3] >> 16) & 0xFF;
                    return precision == 0;

                case double d:
                    return d == Math.Truncate(d);

                case float f:
                    double df = (double)f;
                    return df == Math.Truncate(df);

                default:
                    return false;
            }
        }
    }

    public static class DebugEx
    {
        [Conditional("DEBUG")]
        public static void Trace()
        {
            Debug.WriteLine(CreatePrifix(new StackFrame(1, true)));
        }

        [Conditional("DEBUG")]
        public static void Trace(string format, params object[] args)
        {
            Debug.WriteLine(CreatePrifix(new StackFrame(1, true)) + format, args);
        }

        [Conditional("DEBUG")]
        public static void EnterMethod()
        {
            Debug.WriteLine(CreatePrifix(new StackFrame(1, true)) + "EnterMethod");
        }

        [Conditional("DEBUG")]
        public static void ExitMethod()
        {
            Debug.WriteLine(CreatePrifix(new StackFrame(1, true)) + "ExitMethod");
        }

        static string CreatePrifix(StackFrame sf)
        {
            var method = sf.GetMethod();
            var methodname = method.DeclaringType + "." + method.Name;
            var fileName = Path.GetFileName(sf.GetFileName());
            var lineNum = sf.GetFileLineNumber();
            var threadId = Thread.CurrentThread.ManagedThreadId;
            return $"{fileName}({lineNum}):{methodname}({threadId}) ";
        }
    }
}
