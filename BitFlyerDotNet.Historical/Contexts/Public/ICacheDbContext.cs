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
        IEnumerable<IBfExecution> GetBackwardExecutions(long before, long after);
        void AddExecution(IBfExecution exec);

        // OHLCs
        IEnumerable<IOhlcvv> GetOhlcsBackward(TimeSpan frameSpan, DateTime endFrom, TimeSpan span);
        void AddOhlc(TimeSpan frameSpan, IOhlcvv ohlc);

        DbSet<DbMinuteMarker> Marker { get; }

        void SaveExecutionChanges();
        void SaveOhlcChanges();

        void ClearCache();
    }
}
