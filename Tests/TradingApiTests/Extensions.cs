//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System.Collections.Generic;
using System.Globalization;
using BitFlyerDotNet.Trading;

namespace TradingApiTests
{

    public static class BfxOrderTransactionEventTypeExtensions
    {
        static Dictionary<BfxOrderTransactionEventType, string> TransactionEventNames = new Dictionary<BfxOrderTransactionEventType, string>
        {
            { BfxOrderTransactionEventType.Unknown,             "不明　　　　" },

            { BfxOrderTransactionEventType.OrderSending,        "注文送信中　" },
            { BfxOrderTransactionEventType.OrderSent,           "注文送信完了" },
            { BfxOrderTransactionEventType.OrderSendFailed,     "注文送信失敗" },
            { BfxOrderTransactionEventType.OrderSendCanceled,   "注文送信取消" },
            { BfxOrderTransactionEventType.Ordered,             "発注完了　　" },
            { BfxOrderTransactionEventType.OrderFailed,         "発注失敗　　" },

            { BfxOrderTransactionEventType.PartiallyExecuted,   "一部約定　　" },
            { BfxOrderTransactionEventType.Executed,            "約定　　　　" },
            { BfxOrderTransactionEventType.Triggered,           "条件成立　　" },
            { BfxOrderTransactionEventType.Completed,           "執行完了　　" },

            { BfxOrderTransactionEventType.CancelSending,       "取消送信中　" },
            { BfxOrderTransactionEventType.CancelSent,          "取消送信完了" },
            { BfxOrderTransactionEventType.CancelSendFailed,    "取消送信失敗" },
            { BfxOrderTransactionEventType.CancelSendCanceled,  "取消送信中断" },
            { BfxOrderTransactionEventType.Canceled,            "取消完了　　" },
            { BfxOrderTransactionEventType.CancelFailed,        "取消失敗　　" },

            { BfxOrderTransactionEventType.Expired,             "失効　　　　" },
        };

        static Dictionary<BfxOrderTransactionEventType, string> TransactionChildEventNames = new Dictionary<BfxOrderTransactionEventType, string>
        {
            { BfxOrderTransactionEventType.Ordered,             "執行　　　　" },
            { BfxOrderTransactionEventType.OrderFailed,         "執行失敗　　" },
            { BfxOrderTransactionEventType.PartiallyExecuted,   "一部約定　　" },
            { BfxOrderTransactionEventType.Executed,            "約定　　　　" },
            { BfxOrderTransactionEventType.Canceled,            "執行取消　　" },
            { BfxOrderTransactionEventType.CancelFailed,        "執行取消失敗" },
            { BfxOrderTransactionEventType.Expired,             "失効　　　　" },
        };

        public static string ToDisplayString(this BfxOrderTransactionEventType eventType, CultureInfo ci = null)
        {
            return TransactionEventNames[eventType];
        }

        public static string ToChildDisplayString(this BfxOrderTransactionEventType childEventType, CultureInfo ci = null)
        {
            return TransactionChildEventNames[childEventType];
        }
    }
}
