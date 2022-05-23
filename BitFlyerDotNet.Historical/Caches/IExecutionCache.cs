//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public interface IExecutionCache : IDisposable
    {
        long CommitCount { get; set; }
        void OptimizeManageTable();
        IObservable<IBfExecution> FillGaps(BitFlyerClient client);
        IObservable<IBfExecution> UpdateRecents(BitFlyerClient client);
        void Add(IBfExecution exec);
        void SaveChanges();
        void InsertGap(long before, long after);
    }
}
