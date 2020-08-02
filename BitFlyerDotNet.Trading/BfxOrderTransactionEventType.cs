//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System.Collections.Generic;
using System.Globalization;

namespace BitFlyerDotNet.Trading
{
    public enum BfxOrderTransactionEventType
    {
        Unknown,

        OrderSending,
        OrderSent,
        OrderSendFailed,
        Ordered,
        OrderFailed,

        PartiallyExecuted,
        Executed,
        Triggered,

        CancelSending,
        CancelSent,
        CancelSendFailed,
        Canceled,
        CancelFailed,

        Expired,
    }

    public static class BfxOrderTransactionEventTypeExtensions
    {
        static Dictionary<BfxOrderTransactionEventType, string> TransactionEventNames = new Dictionary<BfxOrderTransactionEventType, string>
        {
            { BfxOrderTransactionEventType.Unknown,             "不明" },

            { BfxOrderTransactionEventType.OrderSending,        "注文送信中" },
            { BfxOrderTransactionEventType.OrderSent,           "注文送信完了" },
            { BfxOrderTransactionEventType.OrderSendFailed,     "注文送信失敗" },
            { BfxOrderTransactionEventType.Ordered,             "注文済" },
            { BfxOrderTransactionEventType.OrderFailed,         "注文失敗" },

            { BfxOrderTransactionEventType.PartiallyExecuted,   "一部約定" },
            { BfxOrderTransactionEventType.Executed,            "約定" },
            { BfxOrderTransactionEventType.Triggered,           "注文執行" },

            { BfxOrderTransactionEventType.CancelSending,       "取消送信中" },
            { BfxOrderTransactionEventType.CancelSent,          "取消送信完了" },
            { BfxOrderTransactionEventType.CancelSendFailed,    "取消送信失敗" },
            { BfxOrderTransactionEventType.Canceled,            "取消済" },
            { BfxOrderTransactionEventType.CancelFailed,        "取消失敗" },

            { BfxOrderTransactionEventType.Expired,             "失効" },
        };

        public static string ToDisplayString(this BfxOrderTransactionEventType eventType, CultureInfo ci = null)
        {
            return TransactionEventNames[eventType];
        }
    }
}
