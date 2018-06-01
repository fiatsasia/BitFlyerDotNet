//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BitFlyerDotNet.LightningApi;

namespace BfAutomatedTradeSample
{
    class Ohlc
    {
        public DateTime Start { get; private set; } = DateTime.MaxValue;
        public double Open { get; private set; }
        public double High { get; private set; } = double.MinValue;
        public double Low { get; private set; } = double.MaxValue;
        public double Close { get; private set;}
        public double Volume { get; private set; }

        public Ohlc(IList<BfExecution> exections)
        {
            var end = DateTime.MinValue;
            foreach (var exec in exections)
            {
                if (Start > exec.ExecutedTime)
                {
                    Start = exec.ExecutedTime;
                    Open = exec.Price;
                }

                High = Math.Max(High, exec.Price);
                Low = Math.Min(Low, exec.Price);

                if (end < exec.ExecutedTime)
                {
                    end = exec.ExecutedTime;
                    Close = exec.Price;
                }

                Volume += exec.Size;
            }
        }
    }
}
