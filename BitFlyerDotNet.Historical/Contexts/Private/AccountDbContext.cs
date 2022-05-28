//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    class AccountDbContext : DbContext
    {
        public DbSet<DbParentOrder> ParentOrders { get; set; }
        public DbSet<DbChildOrder> ChildOrders { get; set; }
        public DbSet<DbPrivateExecution> Executions { get; set; }
        public DbSet<DbCollateral> Collaterals { get; set; }
        public DbSet<DbBalance> Balances { get; set; }

        // EF Core 3.0 bugs cause conflicts System.Linq.Async
        public IQueryable<DbParentOrder> GetParentOrders() => ParentOrders.AsQueryable();
        public IQueryable<DbChildOrder> GetChildOrders() => ChildOrders.AsQueryable();
        public IQueryable<DbPrivateExecution> GetExecutions() => Executions.AsQueryable();
        public IQueryable<DbCollateral> GetCollaterals() => Collaterals.AsQueryable();
        public IQueryable<DbBalance> GetBalances() => Balances.AsQueryable();

        readonly string _connStr;

        public AccountDbContext(string connStr)
            : base(new DbContextOptionsBuilder<AccountDbContext>().Options)
        {
            _connStr = connStr;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbParentOrder>().HasKey(b => new { b.ProductCode, b.AcceptanceId }); // multiple key
            modelBuilder.Entity<DbParentOrder>().HasIndex(b => b.PagingId);
            modelBuilder.Entity<DbParentOrder>().HasIndex(b => b.AcceptanceId);
            modelBuilder.Entity<DbParentOrder>().HasIndex(b => b.OrderId);
            modelBuilder.Entity<DbParentOrder>().HasIndex(b => b.State);
            modelBuilder.Entity<DbParentOrder>().HasIndex(b => b.OrderDate);

            modelBuilder.Entity<DbChildOrder>().Property(e => e.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<DbChildOrder>().HasIndex(b => b.PagingId);
            modelBuilder.Entity<DbChildOrder>().HasIndex(b => b.ProductCode);
            modelBuilder.Entity<DbChildOrder>().HasIndex(b => b.AcceptanceId);
            modelBuilder.Entity<DbChildOrder>().HasIndex(b => b.OrderId);
            modelBuilder.Entity<DbChildOrder>().HasIndex(b => b.State);
            modelBuilder.Entity<DbChildOrder>().HasIndex(b => b.OrderDate);
            modelBuilder.Entity<DbChildOrder>().HasIndex(b => b.ParentOrderAcceptanceId);
            modelBuilder.Entity<DbChildOrder>().HasIndex(b => b.ChildOrderIndex);

            modelBuilder.Entity<DbPrivateExecution>().HasKey(b => new { b.ProductCode, b.ExecutionId }); // multiple key
            modelBuilder.Entity<DbPrivateExecution>().HasIndex(b => b.ChildOrderId);
            modelBuilder.Entity<DbPrivateExecution>().HasIndex(b => b.ExecutedTime);

            modelBuilder.Entity<DbCollateral>().HasIndex(b => b.CurrencyCode);
            modelBuilder.Entity<DbCollateral>().HasIndex(b => b.Date);

            modelBuilder.Entity<DbBalance>().HasIndex(b => b.Date);
            modelBuilder.Entity<DbBalance>().HasIndex(b => b.ProductCode);
            modelBuilder.Entity<DbBalance>().HasIndex(b => b.OrderId);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connStr);
        }

        public DbParentOrder FindParentOrder(string productCode, string acceptanceId) => ParentOrders.Find(productCode, acceptanceId);

        public DbChildOrder FindChildOrder(string productCode, string acceptanceId)
            => GetChildOrders().Where(e => e.ProductCode == productCode && e.AcceptanceId == acceptanceId).FirstOrDefault();

        public DbChildOrder FindChildOrder(string productCode, string parentOrderAcceptanceId, int childOrderIndex)
            => GetChildOrders().Where(e => e.ProductCode == productCode && e.ParentOrderAcceptanceId == parentOrderAcceptanceId && e.ChildOrderIndex == childOrderIndex).FirstOrDefault();

        // Parent orders
        public void Insert(string productCode, BfParentOrder req, BfParentOrderResponse resp)
        {
            ParentOrders.Add(new DbParentOrder(productCode, req, resp));
            for (int childOrderIndex = 0; childOrderIndex < req.Parameters.Count; childOrderIndex++)
            {
                ChildOrders.Add(new DbChildOrder(req, resp, childOrderIndex));
            }
        }

        // Child orders
        public void Insert(BfChildOrder req, BfChildOrderResponse resp)
        {
            ChildOrders.Add(new DbChildOrder(req, resp.ChildOrderAcceptanceId));
        }

        public void Update(string productCode, BfChildOrderStatus order)
        {
            GetChildOrders().Where(e => e.ProductCode == productCode && e.AcceptanceId == order.ChildOrderAcceptanceId).First().Update(order);
        }

        public void Upsert(string productCode, BfChildOrderStatus order)
        {
            var rec = GetChildOrders().Where(e => e.ProductCode == productCode && e.AcceptanceId == order.ChildOrderAcceptanceId).FirstOrDefault();
            if (rec == default) { ChildOrders.Add(new DbChildOrder(productCode, order)); }
            else { rec.Update(order); }
        }

        public void Upsert(string productCode, IEnumerable<BfChildOrderStatus> orders) => orders.ForEach(e => Upsert(productCode, e));

        public void Upsert(string productCode, BfChildOrderStatus order, string parentOrderAcceptanceId, string parentOrderId, int childOrderIndex)
        {
            var rec = GetChildOrders().Where(e => e.ProductCode == productCode && e.AcceptanceId == order.ChildOrderAcceptanceId).FirstOrDefault();
            if (rec == default)
            {
                rec = new DbChildOrder(productCode, order);
                rec.ParentOrderAcceptanceId = parentOrderAcceptanceId;
                rec.ParentOrderId = parentOrderId;
                rec.ChildOrderIndex = childOrderIndex;
                ChildOrders.Add(rec);
            }
            else
            {
                rec.ParentOrderAcceptanceId = parentOrderAcceptanceId;
                rec.ParentOrderId = parentOrderId;
                rec.ChildOrderIndex = childOrderIndex;
                rec.Update(order);
            }
        }

        public void Upsert(string productCode, BfChildOrderStatus order, BfParentOrderDetailStatus detail, int childOrderIndex)
        {
            var rec = GetChildOrders().Where(e => e.ProductCode == productCode && e.AcceptanceId == order.ChildOrderAcceptanceId).FirstOrDefault();
            if (rec == default)
            {
                rec = new DbChildOrder(productCode, detail, childOrderIndex);
                rec.Update(order);
                ChildOrders.Add(rec);
            }
            else
            {
                rec.Update(detail, childOrderIndex);
                rec.Update(order);
            }
        }

        public void InsertIfNotExits(string productCode, BfPrivateExecution exec)
        {
            var rec = GetExecutions().Where(e => e.ExecutionId == exec.ExecutionId).FirstOrDefault();
            if (rec == default)
            {
                Executions.Add(new DbPrivateExecution(productCode, exec));
            }
        }

        public void InsertIfNotExits(string productCode, IEnumerable<BfPrivateExecution> execs) => execs.ForEach(e => InsertIfNotExits(productCode, e));
    }
}
