//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Financial.Extensions;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    interface ICacheDbContext : IDisposable
    {
        BfProductCode ProductCode { get; }

        // Manage table
        List<IManageRecord> GetManageTable();
        void AddManageRecord(IManageRecord manageRec);
        void UpdateManageTable(IEnumerable<IManageRecord> manageRecs);

        // Executions
        IEnumerable<IBfExecution> GetBackwardExecutions();
        IEnumerable<IBfExecution> GetBackwardExecutions(int before, int after);
        void AddExecution(IBfExecution exec);

        // OHLCs
        IEnumerable<IOhlcvv<decimal>> GetOhlcsBackward(TimeSpan frameSpan, DateTime endFrom, TimeSpan span);
        void AddOhlc(TimeSpan frameSpan, IOhlcvv<decimal> ohlc);

        DbSet<DbMinuteMarker> Marker { get; }

        void SaveExecutionChanges();
        void SaveOhlcChanges();

        void ClearCache();
    }
}
