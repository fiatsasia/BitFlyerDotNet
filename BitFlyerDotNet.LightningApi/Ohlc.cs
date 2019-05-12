//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;

namespace BitFlyerDotNet.LightningApi
{
    public interface IBfOhlc
    {
        DateTime Start { get; }
        decimal Open { get; }
        decimal High { get; }
        decimal Low { get; }
        decimal Close { get; }
        decimal Volume { get; }
        decimal VWAP { get; }
    }

    public class BfOhlc : IBfOhlc
    {
        public DateTime Start { get; }
        public decimal Open { get; }
        public decimal High { get; }
        public decimal Low { get; }
        public decimal Close { get; }
        public decimal Volume { get; }
        public decimal VWAP { get; }
    }

    public interface IBfOhlcEx : IBfOhlc
    {
        int ExecutionCount { get; }
        decimal BuyVolume { get; }
        decimal SellVolume { get; }
        decimal ExecutedVolume { get; }
    }
}
