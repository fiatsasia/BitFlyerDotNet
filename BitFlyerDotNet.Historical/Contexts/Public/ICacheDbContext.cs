//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Collections.Generic;

namespace BitFlyerDotNet.Historical
{
    public interface ICacheDbContext : IDisposable
    {
        IQueryable<DbManageRecord> ManageTable { get; }
        IQueryable<DbExecution> Executions { get; }
        IQueryable<DbExecution> GetExecutions(DateTime start, TimeSpan span);
        IQueryable<DbOhlc> GetOhlcs(TimeSpan frameSpan, DateTime start, TimeSpan span);

        DateTime LastExecutionTime { get; }
        DateTime LastOhlcTime { get; }

        void Add(DbManageRecord manageRec);
        void Update(IEnumerable<DbManageRecord> manageRecs);

        void Add(DbExecution exec);
        void Add(DbOhlc ohlc);

        void ClearCache();
        void SaveChanges();
    }
}
