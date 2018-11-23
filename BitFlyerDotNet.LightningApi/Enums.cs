//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace BitFlyerDotNet.LightningApi
{
    public enum BfCurrencyCode
    {
        JPY,
        BTC,
        BCH,
        ETH,
        ETC,
        LTC,
        MONA,
        LSK,
    }

    public enum BfProductCode
    {
        [EnumMember(Value = "")]
        Unknown,
        [EnumMember(Value = "BTC_JPY")]
        BTCJPY,
        [EnumMember(Value = "FX_BTC_JPY")]
        FXBTCJPY,
        [EnumMember(Value = "ETH_BTC")]
        ETHBTC,
        [EnumMember(Value = "BCH_BTC")]
        BCHBTC,
        [EnumMember(Value = "BTC_USD")]
        BTCUSD,
        [EnumMember(Value = "BTC_EUR")]
        BTCEUR,
        [EnumMember(Value = "BTCJPY_MAT1WK")]
        BTCJPYMAT1WK,
        [EnumMember(Value = "BTCJPY_MAT2WK")]
        BTCJPYMAT2WK,
        [EnumMember(Value = "BTCJPY_MAT3M")]
        BTCJPYMAT3M,
    }

    public enum BfTradeSide
    {
        [EnumMember(Value = "")]
        Unknown,
        [EnumMember(Value = "BUY")]
        Buy,
        [EnumMember(Value = "SELL")]
        Sell,
        [EnumMember(Value = "BUYSELL")]
        BuySell,
    }
    public static class BfTradeSideExtensions
    {
        public static BfTradeSide Opposite(this BfTradeSide side)
        {
            if (side != BfTradeSide.Buy && side != BfTradeSide.Sell)
            {
                throw new ArgumentException();
            }
            return side == BfTradeSide.Buy ? BfTradeSide.Sell : BfTradeSide.Buy;
        }
    }

    public enum BfBoardHealth
    {
        [EnumMember(Value = "")]
        Unknown,
        [EnumMember(Value = "NORMAL")]
        Normal,
        [EnumMember(Value = "BUSY")]
        Busy,
        [EnumMember(Value = "VERY BUSY")]
        VeryBusy,
        [EnumMember(Value = "SUPER BUSY")]
        SuperBusy,
        [EnumMember(Value = "NO ORDER")]
        NoOrder,
        [EnumMember(Value = "STOP")]
        Stop,
    }

    public enum BfBoardState
    {
        [EnumMember(Value = "")]
        Unknown,
        [EnumMember(Value = "RUNNING")]
        Running,
        [EnumMember(Value = "CLOSED")]
        Closed,
        [EnumMember(Value = "STARTING")]
        Starting,
        [EnumMember(Value = "PREOPEN")]
        Preopen,
        [EnumMember(Value = "CIRCUIT BREAK")]
        CircuitBreak,
        [EnumMember(Value = "AWAITING SQ")]
        AwaitingSQ,
        [EnumMember(Value = "MATURED")]
        Matured,
    }

    public enum BfTransactionStatus
    {
        [EnumMember(Value = "")]
        Unknown,
        [EnumMember(Value = "PENDING")]
        Pending,
        [EnumMember(Value = "COMPLETED")]
        Completed,
    }

    public enum BfOrderType
    {
        [EnumMember(Value = "")]
        Unknown,
        [EnumMember(Value = "LIMIT")]
        Limit,
        [EnumMember(Value = "MARKET")]
        Market,
        [EnumMember(Value = "STOP")]
        Stop,
        [EnumMember(Value = "STOP_LIMIT")]
        StopLimit,
        [EnumMember(Value = "TRAIL")]
        Trail,
        [EnumMember(Value = "SIMPLE")]
        Simple,
        [EnumMember(Value = "IFD")]
        IFD,
        [EnumMember(Value = "OCO")]
        OCO,
        [EnumMember(Value = "IFDOCO")]
        IFDOCO,
    }

    class DecimalJsonConverter : JsonConverter
    {
        public DecimalJsonConverter() { }
        public override bool CanRead { get { return false; } }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
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
            if (value is decimal)
            {
                decimal decimalValue = (decimal)value;
                int precision = (Decimal.GetBits(decimalValue)[3] >> 16) & 0x000000FF;
                return precision == 0;
            }
            else if (value is float || value is double)
            {
                double doubleValue = (double)value;
                return doubleValue == Math.Truncate(doubleValue);
            }

            return false;
        }
    }

    public enum BfTimeInForce
    {
        NotSpecified, // = GTC is default
        GTC,    // Good 'Till Canceled
        IOC,    // Immediate or Cancel
        FOK,    // Fill or Kill
    }

    public enum BfOrderState
    {
        Unknown,
        [EnumMember(Value = "ACTIVE")]
        Active,
        [EnumMember(Value = "COMPLETED")]
        Completed,
        [EnumMember(Value = "CANCELED")]
        Canceled,
        [EnumMember(Value = "EXPIRED")]
        Expired,
        [EnumMember(Value = "REJECTED")]
        Rejected,
    }
}
