//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive.Linq;

namespace BfAutomatedTradeSample
{
    static class Indicators
    {
        public static IObservable<double> SimpleMovingAverage(this IObservable<double> source, int period)
        {
            return source.Buffer(period, 1).Select(e => e.Average());
        }
    }
}
