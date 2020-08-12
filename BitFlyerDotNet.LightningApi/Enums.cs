//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
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
        XRP,
        BAT,
        XLM,
        XEM,
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

    public static class BfProductCodeEx
    {
        static Dictionary<BfProductCode, int> _priceDecimals = new Dictionary<BfProductCode, int>
        {
            { BfProductCode.BTCJPY, 1 },
            { BfProductCode.FXBTCJPY, 1 },
            { BfProductCode.ETHBTC, 5 },
            { BfProductCode.BCHBTC, 5 },
            { BfProductCode.BTCUSD, 2 },
            { BfProductCode.BTCEUR, 2 },
            { BfProductCode.BTCJPYMAT1WK, 1 },
            { BfProductCode.BTCJPYMAT2WK, 1 },
            { BfProductCode.BTCJPYMAT3M, 1 },
            { BfProductCode.ETHJPY, 1 },
        };

        static Dictionary<string, BfProductCode> _originalTable = new Dictionary<string, BfProductCode>();

        static BfProductCodeEx()
        {
            foreach (BfProductCode e in Enum.GetValues(typeof(BfProductCode)))
            {
                _originalTable.Add(e.ToEnumString(), e);
            }
        }

        public static decimal MinimumOrderSize(this BfProductCode productCode)
        {
            switch (productCode)
            {
                case BfProductCode.FXBTCJPY:
                case BfProductCode.ETHBTC:
                case BfProductCode.BCHBTC:
                    return 0.01m;

                default:
                    return 0.001m;
            }
        }

        public static BfProductCode Parse(string s) => _originalTable[s];

        public static int GetPriceDecimals(this BfProductCode productCode) => _priceDecimals[productCode];
    }

    public enum BfMarketType
    {
        Spot,
        FX,
        Futures,
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

    /// <summary>
    /// Order Type
    /// <see href="https://scrapbox.io/BitFlyerDotNet/OrderType">Online help</see>
    /// </summary>
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

    public static class BfOrderTypeExtension
    {
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

        // Send:
        //   BfChildOrderRequest.ChildOrderType
        public static bool IsChildOrderType(this BfOrderType orderType)
        {
            return
                orderType == BfOrderType.Market ||
                orderType == BfOrderType.Limit;
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

    /// <summary>
    /// Order Event Type
    /// <see href="https://scrapbox.io/BitFlyerDotNet/ChildOrderEvent">Online help</see>
    /// </summary>
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
