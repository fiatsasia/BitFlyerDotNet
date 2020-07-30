//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using Financier;

namespace BitFlyerDotNet.Historical
{
    public interface IOhlcCache
    {
        IEnumerable<IOhlcvv<decimal>> GetOhlcsBackward(DateTime endFrom, TimeSpan span);
        void Add(IOhlcvv<decimal> ohlc);
        void SaveChanges();
    }
}
