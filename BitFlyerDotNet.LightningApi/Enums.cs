﻿//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public static class BfCurrencyCode
{
    public const string JPY = "JPY";
    public const string BTC = "BTC";
    public const string BCH = "BCH";
    public const string ETH = "ETH";
    public const string ETC = "ETC";
    public const string LTC = "LTC";
    public const string MONA = "MONA";
    public const string LSK = "LSK";
    public const string XRP = "XRP";
    public const string BAT = "BAT";
    public const string XLM = "XLM";
    public const string XEM = "XEM";
    public const string XTZ = "XTZ";
    public const string DOT = "DOT";
    public const string LINK = "LINK";
    public const string XYM = "XYM";
}

public static class BfProductCode
{
    public const string BTC_JPY = "BTC_JPY";
    public const string XRP_JPY = "XRP_JPY";
    public const string ETH_JPY = "ETH_JPY";
    public const string XLM_JPY = "XLM_JPY";
    public const string MONA_JPY = "MONA_JPY";
    public const string ETH_BTC = "ETH_BTC";
    public const string BCH_BTC = "BCH_BTC";
    public const string FX_BTC_JPY = "FX_BTC_JPY";
    public const string BTCJPY_MAT1WK = "BTCJPY_MAT1WK";
    public const string BTCJPY_MAT2WK = "BTCJPY_MAT2WK";
    public const string BTCJPY_MAT3M = "BTCJPY_MAT3M";
    public const string BTC_USD = "BTC_USD";
    public const string BTC_EUR = "BTC_EUR";

    static Dictionary<string, (int priceDecimal, decimal minimumOrderSize, string format)> _specs = new()
    {
        { BTC_JPY,      (0, 0.001m, "F0") },
        { XRP_JPY,      (0, 0.001m, "F0") },
        { ETH_JPY,      (0, 0.001m, "F0") },
        { XLM_JPY,      (0, 0.001m, "F0") },
        { MONA_JPY,     (0, 0.001m, "F0") },
        { ETH_BTC,      (5, 0.01m,  "F5") },
        { BCH_BTC,      (5, 0.01m,  "F5") },
        { FX_BTC_JPY,   (0, 0.01m,  "F0") },
        { BTCJPY_MAT1WK,(0, 0.001m, "F0") },
        { BTCJPY_MAT2WK,(0, 0.001m, "F0") },
        { BTCJPY_MAT3M, (0, 0.001m, "F0") },
        { BTC_USD,      (2, 0.001m, "F2") },
        { BTC_EUR,      (2, 0.001m, "F2") },
    };

    static BfProductCode()
    {
    }

    public static int GetPriceDecimals(string productCode) => _specs[productCode].priceDecimal;
    public static decimal GetMinimumOrderSize(string productCode) => _specs[productCode].minimumOrderSize;
    public static decimal RoundPrice(string productCode, decimal price) => Math.Round(price, GetPriceDecimals(productCode));
    public static decimal FixSizeDecimalPoint(string productCode, decimal price) => decimal.Parse(price.ToString(_specs[productCode].format));
}

public enum BfMarketType
{
    Spot,
    FX,
    Futures,
}

public enum BfTradeSide
{
    [EnumMember(Value = "BUY")]
    Buy,
    [EnumMember(Value = "SELL")]
    Sell,
    [EnumMember(Value = "BUYSELL")]
    BuySell,
}

public static class BfTradeSideExtensions
{
    public static BfTradeSide GetOpposite(this BfTradeSide side)
    {
        if (side != BfTradeSide.Buy && side != BfTradeSide.Sell)
        {
            throw new ArgumentException();
        }
        return side == BfTradeSide.Buy ? BfTradeSide.Sell : BfTradeSide.Buy;
    }
}

public enum BfTradeType
{
    [EnumMember(Value = "BUY")]
    Buy,

    [EnumMember(Value = "SELL")]
    Sell,

    [EnumMember(Value = "DEPOSIT")]
    Deposit,

    [EnumMember(Value = "WITHDRAW")]
    Withdraw,

    [EnumMember(Value = "FEE")]
    Fee,

    [EnumMember(Value = "POST_COLL")]
    PostColl,

    [EnumMember(Value = "CANCEL_COLL")]
    CancelColl,

    [EnumMember(Value = "PAYMENT")]
    Payment,

    [EnumMember(Value = "TRANSFER")]
    Transfer,
}

public enum BfHealthState
{
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
    [EnumMember(Value = "PENDING")]
    Pending,
    [EnumMember(Value = "COMPLETED")]
    Completed,
}

/// <summary>
/// Order Type
/// <see href="https://scrapbox.io/BitFlyerDotNet/OrderType">Online help</see>
/// </summary>
public enum BfOrderType
{
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

public static class BfOrderTypeExtension
{
    public static int GetChildCount(this BfOrderType orderType)
    {
        return orderType switch
        {
            BfOrderType.IFDOCO => 3,
            BfOrderType.IFD => 2,
            BfOrderType.OCO => 2,
            BfOrderType.Simple => 1,
            BfOrderType.Stop => 0,
            BfOrderType.StopLimit => 0,
            BfOrderType.Trail => 0,
            BfOrderType.Market => 0,
            BfOrderType.Limit => 0,
            _ => throw new ArgumentException()
        };
    }

    // Send:
    //   BfParentOrderRequest.OrderMethod
    // Receive:
    //   BfParentOrder.ParentOrderType
    //   BfParentOrderDetail.OrderMethod
    //   BfParentOrderEvent.ParentOrderType
    public static bool IsOrderMethod(this BfOrderType orderType)
    {
        return
            orderType == BfOrderType.Simple ||
            orderType == BfOrderType.IFD ||
            orderType == BfOrderType.OCO ||
            orderType == BfOrderType.IFDOCO;
    }

    // Send:
    //   BfParentOrderRequestParameter.ConditionType
    //   BfParentOrderParameter.ConditionType
    // Receive:
    //   BfChildOrder.ChildOrderType
    //   BfChildOrderEvent.ChildOrderType
    //   BfParentOrderEvent.ChildOrderType
    public static bool IsConditionType(this BfOrderType orderType)
    {
        return
            orderType == BfOrderType.Market ||
            orderType == BfOrderType.Limit ||
            orderType == BfOrderType.Stop ||
            orderType == BfOrderType.StopLimit ||
            orderType == BfOrderType.Trail;
    }

    public static bool IsSimpleConditionType(this BfOrderType orderType)
    {
        return
            orderType == BfOrderType.Stop ||
            orderType == BfOrderType.StopLimit ||
            orderType == BfOrderType.Trail;
    }

    // Send:
    //   BfChildOrderRequest.ChildOrderType
    public static bool IsChildOrderType(this BfOrderType orderType)
    {
        return
            orderType == BfOrderType.Market ||
            orderType == BfOrderType.Limit;
    }

    public static bool IsOrderPriceValid(this BfOrderType orderType)
    {
        return
            orderType == BfOrderType.Limit ||
            orderType == BfOrderType.StopLimit;
    }

    public static bool IsTriggerPriceValid(this BfOrderType orderType)
    {
        return
            orderType == BfOrderType.Stop ||
            orderType == BfOrderType.StopLimit;
    }
}

public enum BfTimeInForce
{
    GTC,    // Good 'Till Canceled
    IOC,    // Immediate or Cancel
    FOK,    // Fill or Kill
}

public enum BfOrderState
{
    All,
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

/// <summary>
/// Order Event Type
/// <see href="https://scrapbox.io/BitFlyerDotNet/ChildOrderEvent">Online help</see>
/// </summary>
public enum BfOrderEventType
{
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

public static class BfOrderEventTypeExtension
{
    public static bool IsClosed(this BfOrderEventType eventType)
    {
        return eventType switch
        {
            BfOrderEventType.Cancel => true,
            BfOrderEventType.CancelFailed => true,
            BfOrderEventType.Complete => true,
            BfOrderEventType.Expire => true,
            BfOrderEventType.OrderFailed => true,
            _ => false
        };
    }
}

public enum BfCollateralReason
{
    Post,
    Clearing,
    Cancel,
    SFD,
}
