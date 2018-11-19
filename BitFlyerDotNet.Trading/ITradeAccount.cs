//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Text;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public interface ITradeAccount
    {
        BfProductCode ProductCode { get; }

        int MinuteToExpire { get; set; }
        BfTimeInForce TimeInForce { get; set; }

        void OnOrderStatusChanged(BfTradeOrderState status, IBfTradeOrder order);

        DateTime ServerTime { get; }
        IObservable<BfTicker> TickerSource { get; }
        IObservable<IBfExecution> ExecutionSource { get; }
        BfTicker Ticker { get; }
    }
}
