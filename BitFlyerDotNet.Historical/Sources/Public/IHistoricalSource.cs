//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
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
