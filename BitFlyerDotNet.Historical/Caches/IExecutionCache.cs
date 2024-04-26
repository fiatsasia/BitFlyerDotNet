//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Historical;

public interface IExecutionCache : IDisposable
{
    long CommitCount { get; set; }
    void OptimizeManageTable();
    IObservable<BfExecution> FillGaps(BitFlyerClient client);
    IObservable<BfExecution> UpdateRecents(BitFlyerClient client);
    void Add(BfExecution exec);
    void SaveChanges();
    void InsertGap(long before, long after);
}
