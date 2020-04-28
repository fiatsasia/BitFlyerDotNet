//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public interface IExecutionCache : IDisposable
    {
        int CommitCount { get; set; }
        int CurrentBlockTicks { get; }
        void OptimizeManageTable();
        IObservable<IBfExecution> FillGaps(BitFlyerClient client);
        IObservable<IBfExecution> UpdateRecents(BitFlyerClient client);
        void Add(IBfExecution exec);
        void UpdateMarker(IBfExecution exec);
        void SaveChanges();
        void InsertGap(int before, int after);

        IEnumerable<IBfExecution> GetBackwardExecutions();
        IEnumerable<IBfExecution> GetBackwardExecutions(int before, int after);
        List<IManageRecord> GetManageTable();
    }
}
