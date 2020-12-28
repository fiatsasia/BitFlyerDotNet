//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public delegate void RealtimeOhlcUpdateEventCallback(RealtimeOhlc ohlc);

    public class RealtimeOhlc : IBfOhlc
    {
        // IOhlcvv
        public DateTime Start { get; private set; } = DateTime.MaxValue;
        public decimal Open { get; private set; }
        public decimal High { get; private set; } = decimal.MinValue;
        public decimal Low { get; private set; } = decimal.MaxValue;
        public decimal Close { get; private set; }
        public double Volume { get { return BuyVolume + SellVolume + ExecutedVolume; } }

        // IBfOhlc
        public int ExecutionCount { get; private set; }
        public double VWAP { get; private set; }
        public double BuyVolume { get; private set; }
        public double SellVolume { get; private set; }
        public double ExecutedVolume { get; private set; }

        public object Tag { get; set; }
        public event RealtimeOhlcUpdateEventCallback UpdateEvent;

        public DateTime End { get; private set; } = DateTime.MinValue;
        decimal _amount;

        public RealtimeOhlc(IBfExecution exec)
        {
            Update(exec);
        }

        public RealtimeOhlc Update(IBfExecution exec)
        {
            ExecutionCount++;
            if (exec.ExecutedTime < Start)
            {
                Start = exec.ExecutedTime;
                Open = exec.Price;
            }
            if (exec.ExecutedTime > End)
            {
                End = exec.ExecutedTime;
                Close = exec.Price;
            }

            High = Math.Max(High, exec.Price);
            Low = Math.Min(Low, exec.Price);

            switch (exec.Side)
            {
                case BfTradeSide.Buy:
                    BuyVolume += unchecked((double)exec.Size);
                    break;

                case BfTradeSide.Sell:
                    SellVolume += unchecked((double)exec.Size);
                    break;

                default:
                    ExecutedVolume += unchecked((double)exec.Size);
                    break;
            }

            _amount += exec.Price * exec.Size;
            try
            {
                VWAP = unchecked((double)(_amount / unchecked((decimal)Volume)));
            }
            catch (DivideByZeroException)
            {
                VWAP = 0;
            }

            UpdateEvent?.Invoke(this);
            return this;
        }

        public void CommitFrame()
        {
            UpdateEvent?.Invoke(this);
        }
    }
}
