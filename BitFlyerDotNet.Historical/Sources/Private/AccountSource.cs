//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Generic;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public class AccountSource
    {
        BitFlyerClient _client;
        AccountDbContext _ctx;

        public AccountSource(BitFlyerClient client, string connStr)
        {
            _client = client;
            _ctx = new AccountDbContext(connStr);
        }

        public IEnumerable<DbChildOrder> GetChildOrders(BfProductCode productCode, DateTime start, DateTime end)
        {
            var latestQuery = _ctx.Orders.OrderByDescending(e => e.PagingId).Take(1);
            if (latestQuery.Count() == 0)
            {
                _client.GetChildOrders(productCode, BfOrderState.Completed, 0, null, null, null, e => e.ChildOrderDate >= start)
                    .ForEach(e => _ctx.Orders.Add(new DbChildOrder(e)));
                _ctx.SaveChanges();
            }
            else
            {
                var after = latestQuery.First().PagingId;
                _client.GetChildOrders(productCode, BfOrderState.Completed, 0, null, null, null, e => e.PagingId > after)
                    .ForEach(e => _ctx.Orders.Add(new DbChildOrder(e)));
                _ctx.SaveChanges();

                var oldestQuery = _ctx.Orders.OrderBy(e => e.PagingId).Take(1);
                if (oldestQuery.Count() > 0)
                {
                    var oldest = oldestQuery.First();
                    _client.GetChildOrders(productCode, BfOrderState.Completed, oldest.PagingId, null, null, null, e => e.ChildOrderDate >= start)
                        .ForEach(e => _ctx.Orders.Add(new DbChildOrder(e)));
                    _ctx.SaveChanges();
                }
            }

            return _ctx.Orders.OrderBy(e => e.ChildOrderDate).Where(e => e.ChildOrderDate >= start && e.ChildOrderDate <= end);
        }

        public IEnumerable<DbPrivateExecution> GetExecutions(BfProductCode productCode, DateTime start, DateTime end)
        {
            var latestQuery = _ctx.Executions.OrderByDescending(e => e.ExecutionId).Take(1);
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

                var oldestQuery = _ctx.Executions.OrderBy(e => e.ExecutionId).Take(1);
                if (oldestQuery.Count() > 0)
                {
                    var oldest = oldestQuery.First();
                    _client.GetPrivateExecutions(productCode, oldest.ExecutionId, e => e.ExecutedTime >= start)
                        .ForEach(e => _ctx.Executions.Add(new DbPrivateExecution(productCode, e)));
                    _ctx.SaveChanges();
                }
            }

            return _ctx.Executions.OrderBy(e => e.ExecutedTime).Where(e => e.ExecutedTime >= start && e.ExecutedTime <= end);
        }

        public IEnumerable<DbCollateral> GetCollaterals(DateTime start, DateTime end)
        {
            var latestQuery = _ctx.Collaterals.OrderByDescending(e => e.Id).Take(1);
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

                var oldestQuery = _ctx.Collaterals.OrderBy(e => e.Id).Take(1);
                if (oldestQuery.Count() > 0)
                {
                    var oldest = oldestQuery.First();
                    _client.GetCollateralHistory(oldest.Id, e => e.Date >= start)
                        .ForEach(e => _ctx.Collaterals.Add(new DbCollateral(e)));
                    _ctx.SaveChanges();
                }
            }

            return _ctx.Collaterals.OrderBy(e => e.Date).Where(e => e.Date >= start && e.Date <= end);
        }

        public IEnumerable<DbBalance> GetBalances(BfCurrencyCode currencyCode, DateTime start, DateTime end)
        {
            var latestQuery = _ctx.Balances.OrderByDescending(e => e.Id).Take(1);
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

                var oldestQuery = _ctx.Balances.OrderBy(e => e.Id).Take(1);
                if (oldestQuery.Count() > 0)
                {
                    var oldest = oldestQuery.First();
                    _client.GetBalanceHistory(currencyCode, oldest.Id, e => e.EventDate >= start)
                        .ForEach(e => _ctx.Balances.Add(new DbBalance(e)));
                    _ctx.SaveChanges();
                }
            }

            return _ctx.Balances.OrderBy(e => e.Date).Where(e => e.Date >= start && e.Date <= end);
        }
    }
}
