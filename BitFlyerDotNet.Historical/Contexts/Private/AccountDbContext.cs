//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
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

        readonly string _connStr;

        public AccountDbContext(string connStr)
            : base(new DbContextOptionsBuilder<AccountDbContext>().Options)
        {
            _connStr = connStr;
            this.Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbParentOrder>().HasKey(b => new { b.ProductCode, b.AcceptanceId }); // multiple key
            modelBuilder.Entity<DbParentOrder>().HasIndex(b => b.PagingId);
            modelBuilder.Entity<DbParentOrder>().HasIndex(b => b.AcceptanceId);
            modelBuilder.Entity<DbParentOrder>().HasIndex(b => b.OrderId);
            modelBuilder.Entity<DbParentOrder>().HasIndex(b => b.OrderDate);

            modelBuilder.Entity<DbChildOrder>().Property(e => e.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<DbChildOrder>().HasIndex(b => b.PagingId);
            modelBuilder.Entity<DbChildOrder>().HasIndex(b => b.ProductCode);
            modelBuilder.Entity<DbChildOrder>().HasIndex(b => b.AcceptanceId);
            modelBuilder.Entity<DbChildOrder>().HasIndex(b => b.OrderId);
            modelBuilder.Entity<DbChildOrder>().HasIndex(b => b.OrderDate);
            modelBuilder.Entity<DbChildOrder>().HasIndex(b => b.ParentOrderAcceptanceId);
            modelBuilder.Entity<DbChildOrder>().HasIndex(b => b.ChildOrderIndex);

            modelBuilder.Entity<DbPrivateExecution>().HasIndex(b => b.ProductCode);
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

        public DbParentOrder FindParentOrder(BfProductCode productCode, string acceptanceId) => ParentOrders.Find(productCode, acceptanceId);

        public DbChildOrder FindChildOrder(BfProductCode productCode, string acceptanceId)
            => ChildOrders.Where(e => e.ProductCode == productCode && e.AcceptanceId == acceptanceId).FirstOrDefault();

        public DbChildOrder FindChildOrder(BfProductCode productCode, string parentOrderAcceptanceId, int childOrderIndex)
            => ChildOrders.Where(e => e.ProductCode == productCode && e.ParentOrderAcceptanceId == parentOrderAcceptanceId && e.ChildOrderIndex == childOrderIndex).FirstOrDefault();

        // Parent orders
        public void Insert(BfProductCode productCode, BfParentOrderRequest req, BfParentOrderResponse resp)
        {
            ParentOrders.Add(new DbParentOrder(productCode, req, resp));
            for (int childOrderIndex = 0; childOrderIndex < req.Parameters.Count; childOrderIndex++)
            {
                ChildOrders.Add(new DbChildOrder(req, resp, childOrderIndex));
            }
        }

        // Child orders
        public void Insert(BfChildOrderRequest req, BfChildOrderResponse resp)
        {
            ChildOrders.Add(new DbChildOrder(req, resp.ChildOrderAcceptanceId));
        }

        public void Update(BfProductCode productCode, BfChildOrder order)
        {
            ChildOrders.Where(e => e.ProductCode == productCode && e.AcceptanceId == order.ChildOrderAcceptanceId).First().Update(order);
        }

        public void Upsert(BfProductCode productCode, BfChildOrder order)
        {
            var rec = ChildOrders.Where(e => e.ProductCode == productCode && e.AcceptanceId == order.ChildOrderAcceptanceId).FirstOrDefault();
            if (rec == default) { ChildOrders.Add(new DbChildOrder(productCode, order)); }
            else { rec.Update(order); }
        }

        public void Upsert(BfProductCode productCode, IEnumerable<BfChildOrder> orders) => orders.ForEach(e => Upsert(productCode, e));

        public void Upsert(BfProductCode productCode, BfChildOrder order, string parentOrderAcceptanceId, string parentOrderId, int childOrderIndex)
        {
            var rec = ChildOrders.Where(e => e.ProductCode == productCode && e.AcceptanceId == order.ChildOrderAcceptanceId).FirstOrDefault();
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

        public void InsertIfNotExits(BfProductCode productCode, BfPrivateExecution exec)
        {
            var rec = Executions.Where(e => e.ExecutionId == exec.ExecutionId).FirstOrDefault();
            if (rec == default)
            {
                Executions.Add(new DbPrivateExecution(productCode, exec));
            }
        }

        public void InsertIfNotExits(BfProductCode productCode, IEnumerable<BfPrivateExecution> execs) => execs.ForEach(e => InsertIfNotExits(productCode, e));
    }
}
