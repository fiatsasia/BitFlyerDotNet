//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

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
