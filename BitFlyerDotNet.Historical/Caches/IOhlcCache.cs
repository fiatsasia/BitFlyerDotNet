//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public interface IOhlcCache
    {
        IEnumerable<IBfOhlc> GetOhlcsBackward(DateTime endFrom, TimeSpan span);
        void Add(IBfOhlc ohlc);
        void SaveChanges();
    }
}
