//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public class OrderSource : IBfOrderSource, IDisposable
    {
        BitFlyerClient _client;
        BfProductCode _productCode;
        AccountDbContext _ctx;
        BlockingCollection<Func<bool>> _procQ = new ();
        ConcurrentQueue<Func<bool>> _pendQ = new ();
        Task _procTask;
        bool _exitTask;
        object _txLock = new object();

        public OrderSource(BitFlyerClient client, string connStr, BfProductCode productCode)
        {
            _client = client;
            _productCode = productCode;

            try
            {
                _ctx = new AccountDbContext(connStr);
                _ctx.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
            }

            _procTask = Task.Run(() =>
            {
                try
                {
                    while (!_procQ.IsCompleted)
                    {
                        var proc = _procQ.Take();
                        lock (_txLock)
                        {
                            if (!proc.Invoke())
                            {
                                continue;
                            }
                        }

                        if (_exitTask)
                        {
                            break;
                        }

                        while (_pendQ.TryDequeue(out Func<bool> pendProc))
                        {
                            _procQ.Add(pendProc);
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                }
            });
        }

        public async void Dispose()
        {
            _procQ.Add(() => { _exitTask = true; return true; });
            await _procTask;
        }

        #region Initial updates
        //======================================================================
        // Initial updates
        //======================================================================
        public void UpdateRecentParentOrders(DateTime after)
        {
            foreach (var parent in _client.GetParentOrders(_productCode, after))
            {
                var recParent = _ctx.FindParentOrder(_productCode, parent.ParentOrderAcceptanceId);
                var detail = _client.GetParentOrderDetail(_productCode, parentOrderAcceptanceId: parent.ParentOrderAcceptanceId).GetContent();
                if (recParent != default)
                {
                    recParent.Update(parent, detail); // child の更新は？
                }
                else
                {
                    _ctx.ParentOrders.Add(new DbParentOrder(_productCode, parent, detail));
                }

                var children = new Queue<BfaChildOrder>(_client.GetChildOrders(_productCode, parentOrderId: parent.ParentOrderId).GetContent().OrderBy(e => e.ChildOrderAcceptanceId));
                var baseIndex = -1;
                if (children.Count > 0)
                {
                    baseIndex = int.Parse(children.Peek().ChildOrderAcceptanceId.Split('-')[2]);
                }
                for (int childOrderIndex = 0; childOrderIndex < detail.Parameters.Length; childOrderIndex++)
                {
                    var recChild = _ctx.FindChildOrder(_productCode, parent.ParentOrderAcceptanceId, childOrderIndex);
                    if (recChild == default)
                    {
                        recChild = new DbChildOrder(_productCode, detail, childOrderIndex);
                        _ctx.ChildOrders.Add(recChild);
                    }
                    if (children.Count > 0)
                    {
                        var relativeIndex = int.Parse(children.Peek().ChildOrderAcceptanceId.Split('-')[2]) - baseIndex;
                        if (relativeIndex == childOrderIndex)
                        {
                            recChild.Update(children.Dequeue()); // ** IFDOCO and OCO are incomplete
                        }
                    }
                }
            }
            _ctx.SaveChanges();
        }

        public void UpdateRecentChildOrders(DateTime after)
        {
            _ctx.Upsert(_productCode, _client.GetChildOrders(_productCode, after));
            _ctx.SaveChanges();
        }

        public void UpdateRecentExecutions(DateTime after)
        {
            var execs = _client.GetPrivateExecutions(_productCode, after);
            _ctx.InsertIfNotExits(_productCode, execs);
            _ctx.SaveChanges();
        }

        public void UpdateRecentOrders(DateTime after)
        {
            UpdateRecentParentOrders(after);
            UpdateRecentChildOrders(after);
            UpdateRecentExecutions(after);

            // Recover paret - children missing link
            var parents = _ctx.GetParentOrders().Where(e => e.ProductCode == _productCode && e.OrderDate >= after);
            foreach (var parent in parents)
            {
                var recs = _ctx.GetChildOrders().Where(e => e.ParentOrderAcceptanceId == parent.AcceptanceId).OrderBy(e => e.ChildOrderIndex).ToList();
                if (recs.Count == parent.OrderType.GetChildCount())
                {
                    continue; // Already linked all children
                }

                // ** child-order-index determination is incomplete.
                var children = _client.GetChildOrders(_productCode, parentOrderId: parent.OrderId).GetContent().OrderBy(e => e.ChildOrderDate);
                var childOrderIndex = 0;
                foreach (var child in children)
                {
                    var rec = _ctx.GetChildOrders().Where(e => e.AcceptanceId == child.ChildOrderAcceptanceId).FirstOrDefault();
                    if (rec == default)
                    {
                        _ctx.Upsert(_productCode, child, parent.AcceptanceId, parent.OrderId, childOrderIndex);
                    }
                    else
                    {
                        rec.ParentOrderAcceptanceId = parent.AcceptanceId;
                        rec.ParentOrderId = parent.OrderId;
                        if (rec.ChildOrderIndex == 0 && childOrderIndex != 0)
                        {
                            rec.ChildOrderIndex = childOrderIndex;
                        }
                    }
                    childOrderIndex++;
                }
            }
            _ctx.SaveChanges();
        }
        #endregion Initial updates

        #region Manage active orders
        //======================================================================
        // Manage active orders
        //======================================================================
        void UpdateActiveChildOrders()
        {
            var activeChildren = _client.GetChildOrders(_productCode, BfOrderState.Active).GetContent();
            var inactivatedChildIds = _ctx.GetChildOrders().Where(e => e.State == BfOrderState.Active).Select(e => e.AcceptanceId).ToList()
                .Except(activeChildren.Select(e => e.ChildOrderAcceptanceId));

            // Inactivate child orders
            // * Canceled child order is removed from server store.
            foreach (var inactivatedAcceptanceId in inactivatedChildIds)
            {
                var inactivatedChild = _client.GetChildOrders(_productCode, childOrderAcceptanceId: inactivatedAcceptanceId).GetContent();
                if (inactivatedChild.Length == 0) // Probably canceled
                {
                    var rec = _ctx.GetChildOrders().Where(e => e.AcceptanceId == inactivatedAcceptanceId).First();
                    rec.State = BfOrderState.Canceled;
                }
                else
                {
                    _ctx.Update(_productCode, inactivatedChild.First());
                }
            }

            // Update active child orders
            foreach (var child in activeChildren)
            {
                _ctx.Upsert(_productCode, child);
                var execs = _client.GetPrivateExecutions(_productCode, childOrderAcceptanceId: child.ChildOrderAcceptanceId).GetContent();
                if (execs.Length > 0)
                {
                    _ctx.InsertIfNotExits(_productCode, execs);
                }
            }
            _ctx.SaveChanges();
        }

        void UpdateActiveParentOrders()
        {
            // Update active parent orders, descendants and executions
            var activeParents = _client.GetParentOrders(_productCode, BfOrderState.Active).GetContent();
            var inactivatedParents = _ctx.GetParentOrders().Where(e => e.State == BfOrderState.Active).ToList()
                .Where(e => !activeParents.Any(f => f.ParentOrderAcceptanceId == e.AcceptanceId));

            // Inactivate parent orders
            foreach (var inactiveParent in inactivatedParents)
            {
                var detail = _client.GetParentOrderDetail(_productCode, parentOrderAcceptanceId: inactiveParent.AcceptanceId).GetContent();
                var parent = _client.GetParentOrders(_productCode, count: 1, before: detail.PagingId + 1).GetContent().ToArray()[0];
                inactiveParent.Update(parent, detail);
            }

            // Update active parent orders
            foreach (var parent in activeParents)
            {
                var detail = _client.GetParentOrderDetail(_productCode, parentOrderId: parent.ParentOrderId).GetContent();
                var recParent = _ctx.FindParentOrder(_productCode, parent.ParentOrderAcceptanceId);
                if (recParent == default) // if unmanaged order present
                {
                    _ctx.Add(new DbParentOrder(_productCode, parent, detail));
                }
                else
                {
                    recParent.Update(parent, detail);
                }

                // Matches child orders and parent orders with generating child index.
                // - OCO and only single active child, 
                var children = new Queue<BfaChildOrder>(_client.GetChildOrders(_productCode, parentOrderId: parent.ParentOrderId).GetContent().OrderBy(e => e.ChildOrderAcceptanceId));
                int baseIndex = -1;
                if (children.Count > 0)
                {
                    baseIndex = int.Parse(children.Peek().ChildOrderAcceptanceId.Split('-')[2]); // extract last part of acceptance ID
                }
                for (int childOrderIndex = 0; childOrderIndex < detail.Parameters.Length; childOrderIndex++)
                {
                    if (children.Count > 0)
                    {
                        var relativeIndex = int.Parse(children.Peek().ChildOrderAcceptanceId.Split('-')[2]) - baseIndex;
                        if (relativeIndex == childOrderIndex)
                        {
                            var child = children.Dequeue();
                            _ctx.Upsert(_productCode, child, detail, childOrderIndex);
                            var execs = _client.GetPrivateExecutions(_productCode, childOrderAcceptanceId: child.ChildOrderAcceptanceId).GetContent();
                            if (execs.Length > 0)
                            {
                                _ctx.InsertIfNotExits(_productCode, execs);
                            }
                        }
                    }
                }
            }
            _ctx.SaveChanges();
        }

        public void UpdateActiveOrders() => _procQ.Add(() =>
        {
            UpdateActiveChildOrders();
            UpdateActiveParentOrders();
            return true;
        });
        #endregion Manage active orders

        #region Update parent orders cache
        //======================================================================
        // Update parent orders cache
        //======================================================================
        public void OpenParentOrder(BfParentOrderRequest req, BfParentOrderResponse resp) => _procQ.Add(() =>
        {
            Log.Trace("OpenParentOrder");
            _ctx.Insert(_productCode, req, resp);
            _ctx.SaveChanges();
            return true;
        });

        public void RegisterParentOrderEvent(BfParentOrderEvent poe) => _procQ.Add(() => _registerEvent(poe));
        bool _registerEvent(BfParentOrderEvent poe)
        {
            Log.Trace("RegisterParentOrderEvent");

            // Somtimes event arrives before order opened.
            var parent = _ctx.FindParentOrder(_productCode, poe.ParentOrderAcceptanceId); // Find by key
            if (parent == default)
            {
                Log.Trace("Parent order not found. POE queued.");
                _pendQ.Enqueue(() => _registerEvent(poe)); // pending process
                return false;
            }

            switch (poe.EventType)
            {
                case BfOrderEventType.Order:
                case BfOrderEventType.OrderFailed: // OrderFailedReasonの処理は？
                case BfOrderEventType.Cancel:
                case BfOrderEventType.CancelFailed:
                case BfOrderEventType.Expire:
                    parent.Update(poe);
                    break;

                case BfOrderEventType.Trigger:
                case BfOrderEventType.Complete:
                    parent.Update(poe);
                    _ctx.FindChildOrder(_productCode, poe.ParentOrderAcceptanceId, poe.ChildOrderIndex - 1).Update(poe);
                    break;
            }
            _ctx.SaveChanges();
            return true;
        }
        #endregion Update parent orders cache

        #region Update child orders cache
        //======================================================================
        // Update child orders cache
        //======================================================================
        public void OpenChildOrder(BfChildOrderRequest req, BfChildOrderResponse resp) => _procQ.Add(() =>
        {
            Log.Trace("OpenChildOrder");
            _ctx.Insert(req, resp);
            _ctx.SaveChanges();
            return true;
        });

        public void RegisterChildOrderEvent(BfChildOrderEvent coe) => _procQ.Add(() => _registerEvent(coe));
        bool _registerEvent(BfChildOrderEvent coe)
        {
            Log.Trace("RegisterChildOrderEvent");
            // Somtimes event arrives before order opened.
            var child = _ctx.FindChildOrder(_productCode, coe.ChildOrderAcceptanceId);
            if (child == default)
            {
                if (coe.EventType == BfOrderEventType.CancelFailed)
                {
                    // ParentOrderEvent = completed 以降に発生した IFDOCO/OCO の cancel failed は親注文が不明なため
                    // acceptance ID の部分から兄弟を探し、兄弟の parent order acceptance ID から自分を探す。
                    var key = string.Join("-", coe.ChildOrderAcceptanceId.Split('-').Take(2));
                    var sibling = _ctx.GetChildOrders().Where(e => e.AcceptanceId.StartsWith(key)).FirstOrDefault();
                    if (sibling != default)
                    {
                        var me = _ctx.GetChildOrders().Where(e => e.ParentOrderAcceptanceId == sibling.ParentOrderAcceptanceId && e.AcceptanceId == default).FirstOrDefault();
                        if (me != default)
                        {
                            Log.Trace($"Cancel faile which child order acceptance ID not matched but found parent. COAID:{coe.ChildOrderAcceptanceId}");
                            me.Update(coe);
                            me.ParentOrderId = sibling.ParentOrderId;
                            _ctx.SaveChanges();
                            return true;
                        }
                    }

                    Log.Trace($"Parent order not found. CancelFailed ignored. COAID:{coe.ChildOrderAcceptanceId}");
                    return true;
                }

                Log.Trace($"Parent order not found. COE queued. COAID:{coe.ChildOrderAcceptanceId}");
                _pendQ.Enqueue(() => _registerEvent(coe)); // pending process
                return false;
            }

            switch (coe.EventType)
            {
                case BfOrderEventType.Execution:
                    child.Update(coe);
                    _ctx.Executions.Add(new DbPrivateExecution(_productCode, coe));
                    break;

                case BfOrderEventType.Order:
                case BfOrderEventType.OrderFailed:
                case BfOrderEventType.Cancel:
                case BfOrderEventType.CancelFailed:
                case BfOrderEventType.Expire:
                    child.Update(coe);
                    break;
            }
            _ctx.SaveChanges();
            return true;
        }
        #endregion Update child orders cache

        #region Query cache

        //======================================================================
        // Query cache
        //======================================================================
        public IEnumerable<IBfParentOrder> GetActiveParentOrders()
        {
            lock (_txLock)
            {
                var parents = _ctx.GetParentOrders().Where(e => e.State == BfOrderState.Active).ToArray();
                foreach (var parent in parents)
                {
                    parent.Children = _ctx.GetChildOrders().Where(e => e.ParentOrderAcceptanceId == parent.AcceptanceId).OrderBy(e => e.ChildOrderIndex).ToArray();
                    foreach (var child in parent.Children)
                    {
                        child.Executions = _ctx.GetExecutions().Where(e => e.ChildOrderAcceptanceId == child.AcceptanceId).OrderBy(e => e.ExecutedTime).ToArray();
                    }
                }
                return parents;
            }
        }

        public IEnumerable<IBfChildOrder> GetActiveIndependentChildOrders()
        {
            lock (_txLock)
            {
                var children = _ctx.GetChildOrders().Where(e => e.ParentOrderAcceptanceId == default && e.State == BfOrderState.Active).ToArray();
                foreach (var child in children)
                {
                    child.Executions = _ctx.GetExecutions().Where(e => e.ChildOrderAcceptanceId == child.AcceptanceId).ToArray();
                }
                return children;
            }
        }

        public IBfParentOrder GetParentOrder(string parentOrderAcceptanceId)
        {
            lock (_txLock)
            {
                var parent = _ctx.FindParentOrder(_productCode, parentOrderAcceptanceId);
                if (parent == default)
                {
                    return default;
                }

                parent.Children = _ctx.GetChildOrders().Where(e => e.ParentOrderAcceptanceId == parent.AcceptanceId).ToArray();
                foreach (var child in parent.Children)
                {
                    child.Executions = _ctx.GetExecutions().Where(e => e.ChildOrderAcceptanceId == child.AcceptanceId).ToArray();
                }
                return parent;
            }
        }

        public IBfChildOrder GetChildOrder(string childOrderAcceptanceId)
        {
            lock (_txLock)
            {
                var child = _ctx.GetChildOrders().Where(e => e.AcceptanceId == childOrderAcceptanceId).FirstOrDefault();
                if (child == default)
                {
                    return default;
                }
                child.Executions = _ctx.GetExecutions().Where(e => e.ChildOrderAcceptanceId == childOrderAcceptanceId).OrderByDescending(e => e.ExecutedTime).ToArray();
                return child;
            }
        }

        public decimal CalculateProfit(TimeSpan span)
        {
            lock (_txLock)
            {
                var until = DateTime.UtcNow - span;
                var execs = _ctx.GetExecutions().Where(e => e.ProductCode == _productCode && e.ExecutedTime >= until).OrderBy(e => e.ExecutedTime);
                return execs.ToList().Select(exec => exec.Amount * (exec.Side == BfTradeSide.Buy ? -1m : 1m)).Sum();
            }
        }
        #endregion Query cache

        //======================================================================
        // WIPs
        //======================================================================
        // Private executions
        public IEnumerable<DbPrivateExecution> GetExecutions(BfProductCode productCode, DateTime start, DateTime end)
        {
            var latestQuery = _ctx.GetExecutions().OrderByDescending(e => e.ExecutionId).Take(1);
            if (latestQuery.Count() == 0)
            {
                _client.GetPrivateExecutions(productCode, 0, e => e.ExecutedTime >= start)
                    .ForEach(e => _ctx.Executions.Add(new DbPrivateExecution(productCode, e)));
                _ctx.SaveChanges();
            }
            else
            {
                var after = latestQuery.First().ExecutionId;
                _client.GetPrivateExecutions(productCode, 0, e => e.ExecutionId > after)
                    .ForEach(e => _ctx.Executions.Add(new DbPrivateExecution(productCode, e)));
                _ctx.SaveChanges();

                var oldestQuery = _ctx.GetExecutions().OrderBy(e => e.ExecutionId).Take(1);
                if (oldestQuery.Count() > 0)
                {
                    var oldest = oldestQuery.First();
                    _client.GetPrivateExecutions(productCode, oldest.ExecutionId, e => e.ExecutedTime >= start)
                        .ForEach(e => _ctx.Executions.Add(new DbPrivateExecution(productCode, e)));
                    _ctx.SaveChanges();
                }
            }

            return _ctx.GetExecutions().OrderBy(e => e.ExecutedTime).Where(e => e.ExecutedTime >= start && e.ExecutedTime <= end);
        }

        public IEnumerable<DbCollateral> GetCollaterals(DateTime start, DateTime end)
        {
            var latestQuery = _ctx.GetCollaterals().OrderByDescending(e => e.Id).Take(1);
            if (latestQuery.Count() == 0)
            {
                _client.GetCollateralHistory(0, e => e.Date >= start)
                    .ForEach(e => _ctx.Collaterals.Add(new DbCollateral(e)));
                _ctx.SaveChanges();
            }
            else
            {
                var after = latestQuery.First().Id;
                _client.GetCollateralHistory(0, e => e.PagingId > after)
                    .ForEach(e => _ctx.Collaterals.Add(new DbCollateral(e)));
                _ctx.SaveChanges();

                var oldestQuery = _ctx.GetCollaterals().OrderBy(e => e.Id).Take(1);
                if (oldestQuery.Count() > 0)
                {
                    var oldest = oldestQuery.First();
                    _client.GetCollateralHistory(oldest.Id, e => e.Date >= start)
                        .ForEach(e => _ctx.Collaterals.Add(new DbCollateral(e)));
                    _ctx.SaveChanges();
                }
            }

            return _ctx.GetCollaterals().OrderBy(e => e.Date).Where(e => e.Date >= start && e.Date <= end);
        }

        public IEnumerable<DbBalance> GetBalances(BfCurrencyCode currencyCode, DateTime start, DateTime end)
        {
            var latestQuery = _ctx.GetBalances().OrderByDescending(e => e.Id).Take(1);
            if (latestQuery.Count() == 0)
            {
                _client.GetBalanceHistory(currencyCode, 0, e => e.EventDate >= start)
                    .ForEach(e => _ctx.Balances.Add(new DbBalance(e)));
                _ctx.SaveChanges();
            }
            else
            {
                var after = latestQuery.First().Id;
                _client.GetBalanceHistory(currencyCode, 0, e => e.PagingId > after)
                    .ForEach(e => _ctx.Balances.Add(new DbBalance(e)));
                _ctx.SaveChanges();

                var oldestQuery = _ctx.GetBalances().OrderBy(e => e.Id).Take(1);
                if (oldestQuery.Count() > 0)
                {
                    var oldest = oldestQuery.First();
                    _client.GetBalanceHistory(currencyCode, oldest.Id, e => e.EventDate >= start)
                        .ForEach(e => _ctx.Balances.Add(new DbBalance(e)));
                    _ctx.SaveChanges();
                }
            }

            return _ctx.GetBalances().OrderBy(e => e.Date).Where(e => e.Date >= start && e.Date <= end);
        }
    }
}
