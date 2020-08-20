//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;

namespace BitFlyerDotNet.Historical
{
    class OhlcCache : IOhlcCache
    {
        ICacheDbContext _ctx;
        TimeSpan _frameSpan;

        public OhlcCache(ICacheDbContext ctx, TimeSpan frameSpan)
        {
            _ctx = ctx;
            _frameSpan = frameSpan;
        }

        public void SaveChanges()
        {
            _ctx.SaveOhlcChanges();
        }

        public IEnumerable<IOhlcvv> GetOhlcsBackward(DateTime endFrom, TimeSpan span)
        {
            return _ctx.GetOhlcsBackward(_frameSpan, endFrom, span);
        }

        public void Add(IOhlcvv ohlc)
        {
            _ctx.AddOhlc(_frameSpan, ohlc);
        }
    }
}
