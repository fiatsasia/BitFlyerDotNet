﻿//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Runtime.Serialization;

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
        [EnumMember(Value = "ETH_JPY")]
        ETHJPY,
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

    public enum BfHealthState
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

    public enum BfOrderEventType
    {
        Unknown,
        [EnumMember(Value = "ORDER")]
        Order,
        [EnumMember(Value = "ORDER_FAILED")]
        OrderFailed,
        [EnumMember(Value = "CANCEL")]
        Cancel,
        [EnumMember(Value = "CANCEL_FAILED")]
        CancelFailed,
        [EnumMember(Value = "EXECUTION")]
        Execution,
        [EnumMember(Value = "TRIGGER")]
        Trigger,
        [EnumMember(Value = "COMPLETE")]
        Complete,
        [EnumMember(Value = "EXPIRE")]
        Expire,
    }
}
