//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Reactive.Linq;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    class ExecutionCache : IExecutionCache
    {
        public long CommitCount { get; set; } = 10000;
        readonly ICacheDbContext _dbctx;
        readonly BfProductCode _productCode;

        public ExecutionCache(ICacheDbContext dbctx, BfProductCode productCode)
        {
            _dbctx = dbctx;
            _productCode = productCode;
            OptimizeManageTable();
        }

        public void Dispose()
        {
            _dbctx.Dispose();
        }

        public void OptimizeManageTable()
        {
            var manageRecords = _dbctx.ManageTable.ToList();

            int index = 1;
            if (manageRecords.Count >= 3)
            {
                while (index < manageRecords.Count() - 1)
                {
                    var after = manageRecords[index - 1];
                    var current = manageRecords[index];
                    var before = manageRecords[index + 1];

                    if (current.TransactionKind != "I")
                    {
                        index++;
                        continue;
                    }

                    manageRecords.Remove(after);
                    manageRecords.Remove(current);

                    before.EndExecutionId = after.EndExecutionId;
                    before.EndExecutedTime = after.EndExecutedTime;
                    before.ExecutionCount += after.ExecutionCount + current.ExecutionCount;
                    before.TransactionKind = "M";
                    before.LastUpdatedTime = DateTime.UtcNow;
                }
            }

            index = 0;
            if (manageRecords.Count >= 2)
            {
                while (index < manageRecords.Count() - 1)
                {
                    var current = manageRecords[index];
                    var before = manageRecords[index + 1];

                    if (current.StartExecutionId - before.EndExecutionId == 1)
                    {
                        before.EndExecutionId = current.EndExecutionId;
                        before.EndExecutedTime = current.EndExecutedTime;
                        before.ExecutionCount += current.ExecutionCount;
                        before.TransactionKind = "M";
                        before.LastUpdatedTime = DateTime.UtcNow;
                        manageRecords.Remove(current);
                        continue;
                    }

                    index++;
                }
            }
            _dbctx.Update(manageRecords);
            _dbctx.SaveChanges();
        }

        public IObservable<IBfExecution> FillGaps(BitFlyerClient client)
        {
            return _dbctx.ManageTable.Buffer(2, 1).SkipLast(1).Select(rec =>
            {
                var count = 0;
                return new HistoricalExecutionSource(client, _productCode, rec[0].StartExecutionId, rec[1].EndExecutionId)
                .Select(exec => { count++; return exec; })
                .Finally(() =>
                {
                    if (count == 0)
                    {
                        InsertGap(rec[0].StartExecutionId, rec[1].EndExecutionId);
                    }
                });
            }).Concat().Select(exec =>
            {
                Add(exec);
                return exec;
            })
            .Finally(() =>
            {
                SaveChanges();
            });
        }

        public IObservable<IBfExecution> UpdateRecents(BitFlyerClient client)
        {
            var after = 0L;
            var manageRec = _dbctx.ManageTable.ToList();
            if (manageRec.Count > 0)
            {
                after = manageRec[0].EndExecutionId;
            }

            return new HistoricalExecutionSource(client, _productCode, 0, after)
            .Select(exec =>
            {
                Add(exec);
                return exec;
            })
            .Finally(() =>
            {
                SaveChanges();
            });
        }

        DbManageRecord _manageRec;

        public void Add(IBfExecution exec)
        {
            if (_manageRec == null)
            {
                _manageRec = new DbManageRecord();
            }
            _dbctx.Add(new DbExecution(exec));
            _manageRec.Update(exec);

            if (_manageRec.ExecutionCount >= CommitCount)
            {
                SaveChanges();
                _dbctx.ClearCache();
            }
        }

        public void SaveChanges()
        {
            _dbctx.SaveChanges();

            if (_manageRec == null)
            {
                return;
            }

            Log.Trace($"HistoricalCache committing executions... {_manageRec.StartExecutedTime.ToLocalTime()} - {_manageRec.EndExecutedTime.ToLocalTime()}");
            _dbctx.Add(_manageRec);
            _dbctx.SaveChanges();
            _manageRec = null;
            Log.Trace("HistoricalCache committed.");
        }

        public void InsertGap(long before, long after)
        {
            if (before == 0 || after == 0)
            {
                throw new ArgumentException();
            }
            Log.Trace("HistoricalCache committing executions...");

            var blockRow = new DbManageRecord();
            blockRow.StartExecutionId = after + 1;
            blockRow.EndExecutionId = before - 1;
            blockRow.StartExecutedTime = blockRow.EndExecutedTime = blockRow.CreatedTime;
            blockRow.TransactionKind = "I";
            _dbctx.Add(blockRow);
            _dbctx.SaveChanges();

            Log.Trace("HistoricalCache committed.");
        }
    }
}
