//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public delegate void RealtimeOhlcUpdateEventCallback(RealtimeOhlc ohlc);

    public class RealtimeOhlc : IBfOhlcEx
    {
        // IBfOhlc
        public DateTime Start { get; private set; } = DateTime.MaxValue;
        public decimal Open { get; private set; }
        public decimal High { get; private set; } = decimal.MinValue;
        public decimal Low { get; private set; } = decimal.MaxValue;
        public decimal Close { get; private set; }
        public decimal Volume { get { return BuyVolume + SellVolume + ExecutedVolume; } }

        // IBfOhlcEx
        public int ExecutionCount { get; private set; }
        public decimal VWAP { get; private set; }
        public decimal BuyVolume { get; private set; }
        public decimal SellVolume { get; private set; }
        public decimal ExecutedVolume { get; private set; }

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
                    BuyVolume += exec.Size;
                    break;

                case BfTradeSide.Sell:
                    SellVolume += exec.Size;
                    break;

                default:
                    ExecutedVolume += exec.Size;
                    break;
            }

            _amount += exec.Price * exec.Size;
            try
            {
                VWAP = _amount / Volume;
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
