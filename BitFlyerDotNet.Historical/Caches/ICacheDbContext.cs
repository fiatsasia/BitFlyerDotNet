//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All right reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    interface ICacheDbContext
    {
        BfProductCode ProductCode { get; }

        // Manage table
        List<IManageRecord> GetManageTable();
        void AddManageRecord(IManageRecord manageRec);
        void UpdateManageTable(IEnumerable<IManageRecord> manageRecs);

        // Executions
        IEnumerable<IBfExecution> GetBackwardExecutions(int before, int after);
        void AddExecution(IBfExecution exec);

        // OHLCs
        IEnumerable<IBfOhlc> GetOhlcsBackward(TimeSpan frameSpan, DateTime endFrom, TimeSpan span);
        void AddOhlc(TimeSpan frameSpan, IBfOhlc ohlc);

        DbSet<DbMinuteMarker> Marker { get; }

        void SaveExecutionChanges();
        void SaveOhlcChanges();

        void ClearCache();
    }
}
