//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Fiats.Utils;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    class HistoricalExecutionCache
    {
        ExecutionBlockDbContext _ctxManage;
        ExecutionDbContext _ctxExec;
        ExecutionMinuteMarketDbContext _ctxMarker;

        public DbSet<DbExecutionTickRow> Executions { get { return _ctxExec.Instance; } }
        public DbSet<ExecutionMinuteMarkerRow> Marker { get { return _ctxMarker.Instance; } }

        public HistoricalExecutionCache(BfProductCode productCode, string cacheFolderBasePath)
        {
            _ctxManage = new ExecutionBlockDbContext(productCode, cacheFolderBasePath, "MANAGE");
            _ctxExec = new ExecutionDbContext(productCode, cacheFolderBasePath, "EXEC");
            _ctxMarker = new ExecutionMinuteMarketDbContext(productCode, cacheFolderBasePath, "MARKER");

            MergeBlocks();
        }

        void MergeBlocks()
        {
            var blocks = GetManageBlocks();

            int index = 1;
            if (blocks.Count >= 3)
            {
                while (index < blocks.Count() - 1)
                {
                    var after = blocks[index - 1];
                    var current = blocks[index];
                    var before = blocks[index + 1];

                    if (current.TransactionKind != "I")
                    {
                        index++;
                        continue;
                    }

                    blocks.Remove(after);
                    blocks.Remove(current);

                    before.EndTickId = after.EndTickId;
                    before.EndTickTime = after.EndTickTime;
                    before.Ticks += after.Ticks + current.Ticks;
                    before.TransactionKind = "M";
                    before.LastUpdatedTime = DateTime.UtcNow;
                }
            }

            index = 0;
            if (blocks.Count >= 2)
            {
                while (index < blocks.Count() - 1)
                {
                    var current = blocks[index];
                    var before = blocks[index + 1];

                    if (current.StartTickId - before.EndTickId == 1)
                    {
                        before.EndTickId = current.EndTickId;
                        before.EndTickTime = current.EndTickTime;
                        before.Ticks += current.Ticks;
                        before.TransactionKind = "M";
                        before.LastUpdatedTime = DateTime.UtcNow;
                        blocks.Remove(current);
                        continue;
                    }

                    index++;
                }
            }
            _ctxManage.Instance.RemoveRange(_ctxManage.Instance);
            _ctxManage.Instance.AddRange(blocks);
            _ctxManage.SaveChanges();
        }

        DbExecutionBlocksRow _manageRow;

        public int CurrentBlockTicks { get { return _manageRow == null ? 0 : _manageRow.Ticks; } }

        public IList<DbExecutionBlocksRow> GetManageBlocks()
        {
            return _ctxManage.Instance.OrderByDescending(e => e.StartTickId).ToList();
        }

        public void AddExecution(DbExecutionTickRow tick)
        {
            if (_manageRow == null)
            {
                _manageRow = new DbExecutionBlocksRow();
            }
            _ctxExec.Instance.Add(tick);
            _manageRow.Update(tick);

            if (_manageRow.Ticks >= CommitCount)
            {
                CommitCache();
            }
        }

        ExecutionMinuteMarkerRow _marker;
        IBfExecution _lastTick = null;
        static readonly TimeSpan MarkerSpan = TimeSpan.FromMinutes(1);
        public void UpdateCache(IBfExecution tick)
        {
            if (_lastTick == null)
            {
                _lastTick = tick;
                return;
            }

            var lastMarkTime = _lastTick.ExecutedTime.Round(MarkerSpan);
            var currentMarkTime = tick.ExecutedTime.Round(MarkerSpan);
            if (lastMarkTime != currentMarkTime)
            {
                // Marked time changed
                if (_marker != null)
                {
                    if (!_ctxMarker.Instance.Any(e => e.MarkedTime == _marker.MarkedTime))
                    {
                        _marker.StartTickId = _lastTick.ExecutionId;
                        _ctxMarker.Instance.Add(_marker);
                    }
                }

                _marker = new ExecutionMinuteMarkerRow
                {
                    MarkedTime = currentMarkTime,
                    EndTickId = tick.ExecutionId
                };
            }

            if (_marker != null)
            {
                _marker.TickCount++;
            }

            _lastTick = tick;
        }

        const int CommitCount = 10000;
        public void ClearExecutionCache()
        {
            //_ctxMarker.ClearCache();
        }

        public void CommitCache()
        {
            _ctxMarker.SaveChanges();

            if (_manageRow == null)
            {
                return;
            }

            Debug.WriteLine("HistoricalCache committing executions...");
            _ctxManage.Instance.Add(_manageRow);
            _ctxManage.SaveChanges();
            _ctxExec.SaveChanges();
            //_ctxExec.ClearCache();
            _manageRow = null;
            Debug.WriteLine("HistoricalCache committed.");
        }

        public void InsertEmptyBlock(int before, int after)
        {
            Debug.Assert(before != 0 && after != 0);
            Debug.WriteLine("HistoricalCache committing executions...");

            var blockRow = new DbExecutionBlocksRow();
            blockRow.StartTickId = after + 1;
            blockRow.EndTickId = before - 1;
            blockRow.StartTickTime = blockRow.EndTickTime = blockRow.CreatedTime;
            blockRow.TransactionKind = "I";
            _ctxManage.Instance.Add(blockRow);

            _ctxManage.SaveChanges();
            _ctxExec.SaveChanges();
            //_ctxExec.ClearCache();

            Debug.WriteLine("HistoricalCache committed.");
        }
    }
}
