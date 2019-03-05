//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using BitFlyerDotNet.LightningApi;

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

        public IEnumerable<IBfOhlc> GetOhlcsBackward(DateTime endFrom, TimeSpan span)
        {
            return _ctx.GetOhlcsBackward(_frameSpan, endFrom, span);
        }

        public void Add(IBfOhlc ohlc)
        {
            _ctx.AddOhlc(_frameSpan, ohlc);
        }
    }
}
