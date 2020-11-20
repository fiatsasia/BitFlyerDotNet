//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BitFlyerDotNet.Historical
{
    public interface IHistoricalSource<TRecord> where TRecord : class
    {
        DbSet<TRecord> Table { get; }

        void UpdateRecent();
        IEnumerable<TRecord> Get(DateTime start, DateTime end);

    }
}
