//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Reactive.Linq;
using Fiats.Utils;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    class ExecutionCache : IExecutionCache
    {
        readonly ICacheDbContext _ctx;

        public ExecutionCache(ICacheDbContext ctx)
        {
            _ctx = ctx;
            OptimizeManageTable();
        }

        public IEnumerable<IBfExecution> GetBackwardExecutions(int before, int after) { return _ctx.GetBackwardExecutions(before, after); }
        public List<IManageRecord> GetManageTable() { return _ctx.GetManageTable(); }

        public void OptimizeManageTable()
        {
            var manageRecords = _ctx.GetManageTable();

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
            _ctx.UpdateManageTable(manageRecords);
            _ctx.SaveExecutionChanges();
        }

        public IObservable<IBfExecution> FillGaps(BitFlyerClient client)
        {
            return _ctx.GetManageTable().Buffer(2, 1).SkipLast(1).Select(b =>
            {
                return new HistoricalExecutionSource(client, _ctx.ProductCode, b[0].StartExecutionId, b[1].EndExecutionId).Finally(() => SaveChanges());
            }).Merge().Select(exec =>
            {
                Add(exec);
                return exec;
            })
            .Finally(() =>
            {
                SaveChanges();
                OptimizeManageTable();
            });
        }

        public IObservable<IBfExecution> UpdateRecents(BitFlyerClient client)
        {
            var after = 0;
            var manageRec = _ctx.GetManageTable();
            if (manageRec.Count > 0)
            {
                after = manageRec[0].EndExecutionId;
            }

            return new HistoricalExecutionSource(client, _ctx.ProductCode, 0, after)
            .Select(exec =>
            {
                Add(exec);
                return exec;
            })
            .Finally(() =>
            {
                SaveChanges();
                OptimizeManageTable();
            });
        }

        DbManageRecord _manageRec;

        public int CurrentBlockTicks { get { return _manageRec == null ? 0 : _manageRec.ExecutionCount; } }

        public void Add(IBfExecution exec)
        {
            if (_manageRec == null)
            {
                _manageRec = new DbManageRecord();
            }
            _ctx.AddExecution(new DbExecution(exec));
            _manageRec.Update(exec);

            if (_manageRec.ExecutionCount >= CommitCount)
            {
                SaveChanges();
            }
        }

        DbMinuteMarker _marker;
        IBfExecution _lastExec = null;
        static readonly TimeSpan MarkerSpan = TimeSpan.FromMinutes(1);
        public void UpdateMarker(IBfExecution exec)
        {
            if (_lastExec == null)
            {
                _lastExec = exec;
                return;
            }

            var lastMarkTime = _lastExec.ExecutedTime.Round(MarkerSpan);
            var currentMarkTime = exec.ExecutedTime.Round(MarkerSpan);
            if (lastMarkTime != currentMarkTime)
            {
                // Marked time changed
                if (_marker != null)
                {
                    if (!_ctx.Marker.Any(e => e.MarkedTime == _marker.MarkedTime))
                    {
                        _marker.StartExecutionId = _lastExec.ExecutionId;
                        _ctx.Marker.Add(_marker);
                    }
                }

                _marker = new DbMinuteMarker
                {
                    MarkedTime = currentMarkTime,
                    EndExecutionId = exec.ExecutionId
                };
            }

            if (_marker != null)
            {
                _marker.ExecutionCount++;
            }

            _lastExec = exec;
        }

        const int CommitCount = 10000;
        public void ClearCache()
        {
            _ctx.ClearCache();
        }

        public void SaveChanges()
        {
            _ctx.SaveExecutionChanges();

            if (_manageRec == null)
            {
                return;
            }

            Debug.WriteLine("HistoricalCache committing executions... {0} - {1}", _manageRec.StartExecutedTime.ToLocalTime(), _manageRec.EndExecutedTime.ToLocalTime());
            _ctx.AddManageRecord(_manageRec);
            _ctx.SaveExecutionChanges();
            _ctx.ClearCache();
            _manageRec = null;
            Debug.WriteLine("HistoricalCache committed.");
        }

        public void InsertGap(int before, int after)
        {
            Debug.Assert(before != 0 && after != 0);
            Debug.WriteLine("HistoricalCache committing executions...");

            var blockRow = new DbManageRecord();
            blockRow.StartExecutionId = after + 1;
            blockRow.EndExecutionId = before - 1;
            blockRow.StartExecutedTime = blockRow.EndExecutedTime = blockRow.CreatedTime;
            blockRow.TransactionKind = "I";
            _ctx.AddManageRecord(blockRow);
            _ctx.SaveExecutionChanges();
            _ctx.ClearCache();

            Debug.WriteLine("HistoricalCache committed.");
        }
    }
}
