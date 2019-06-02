//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using Financial.Extensions;

namespace BitFlyerDotNet.Historical
{
    public interface IOhlcCache
    {
        IEnumerable<IFxOhlcvv> GetOhlcsBackward(DateTime endFrom, TimeSpan span);
        void Add(IFxOhlcvv ohlc);
        void SaveChanges();
    }
}
