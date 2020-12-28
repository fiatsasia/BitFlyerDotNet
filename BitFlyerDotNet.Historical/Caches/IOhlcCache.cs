//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
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
