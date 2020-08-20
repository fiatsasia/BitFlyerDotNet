//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;

namespace BitFlyerDotNet.Historical
{
    public interface IOhlcCache
    {
        IEnumerable<IOhlcvv> GetOhlcsBackward(DateTime endFrom, TimeSpan span);
        void Add(IOhlcvv ohlc);
        void SaveChanges();
    }
}
