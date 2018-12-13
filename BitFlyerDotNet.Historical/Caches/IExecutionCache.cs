//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All right reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public interface IExecutionCache
    {
        int CurrentBlockTicks { get; }
        void OptimizeManageTable();
        IObservable<IBfExecution> FillGaps(BitFlyerClient client);
        IObservable<IBfExecution> UpdateRecents(BitFlyerClient client);
        void ClearCache();
        void Add(IBfExecution exec);
        void UpdateMarker(IBfExecution exec);
        void SaveChanges();
        void InsertGap(int before, int after);

        IEnumerable<IBfExecution> GetBackwardExecutions(int before, int after);
        List<IManageRecord> GetManageTable();
    }
}
