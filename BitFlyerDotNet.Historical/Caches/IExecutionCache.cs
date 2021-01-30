//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
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
        void InsertGap(long before, long after);

        IEnumerable<IBfExecution> GetBackwardExecutions();
        IEnumerable<IBfExecution> GetBackwardExecutions(long before, long after);
        List<IManageRecord> GetManageTable();
    }
}
