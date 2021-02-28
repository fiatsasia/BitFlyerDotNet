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
using Microsoft.EntityFrameworkCore;
using BitFlyerDotNet.LightningApi;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BitFlyerDotNet.Historical
{
    class BfDbContextSqlServer : DbContext
    {
        static readonly TimeSpan CommandTimeout = TimeSpan.FromHours(1);
        public readonly string ConnStr;
        public readonly BfProductCode ProductCode;

        public DbSet<DbManageRecord> ManageTable { get; set; }
        public DbSet<DbExecution> Executions { get; set; }
        public DbSet<DbOhlc> Ohlc { get; set; }

        public BfDbContextSqlServer(string connStr, BfProductCode productCode)
            : base(new DbContextOptionsBuilder<BfDbContextSqlServer>().Options)
        {
            ConnStr = connStr;
            ProductCode = productCode;
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            Database.SetCommandTimeout(CommandTimeout);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<DbManageRecord>().ToTable($"{ProductCode}_{nameof(ManageTable)}");
            modelBuilder.Entity<DbExecution>().ToTable($"{ProductCode}_{nameof(Executions)}");
            modelBuilder.Entity<DbOhlc>().ToTable($"{ProductCode}_{nameof(Ohlc)}");
            modelBuilder.Entity<DbOhlc>().HasKey(c => new { c.FrameSpanSeconds, c.Start });
        }

        public class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
        {
            public object Create(DbContext ctx) => ctx is BfDbContextSqlServer dynctx ? (ctx.GetType(), dynctx.ProductCode) : ctx.GetType();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlServer(ConnStr)
                .ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory>();
        }
    }

    public class SqlServerDbContext : ICacheDbContext
    {
        public IQueryable<DbManageRecord> ManageTable => _ctx.ManageTable.OrderByDescending(e => e.StartExecutionId);
        public IQueryable<DbExecution> Executions => _ctx.Executions.AsNoTracking();
        public IQueryable<DbOhlc> GetOhlcs(TimeSpan period)
        {
            var sec = Convert.ToInt32(period.TotalSeconds); // to optimize for query builder
            return _ctx.Ohlc.Where(e => e.FrameSpanSeconds == sec).OrderBy(e => e.Start);
        }

        readonly string _connStr;
        readonly BfProductCode _productCode;

        BfDbContextSqlServer _ctx;

        public SqlServerDbContext(string connStr, BfProductCode productCode)
        {
            _connStr = connStr;
            _productCode = productCode;
            _ctx = new BfDbContextSqlServer(connStr, productCode);
        }

        public void Dispose()
        {
            _ctx.Dispose();
        }

        public void ClearCache()
        {
            _ctx.Dispose();
            _ctx = new BfDbContextSqlServer(_connStr, _productCode);
        }

        public DateTime LastExecutionTime => ManageTable.First().EndExecutedTime;
        public DateTime LastOhlcTime => _ctx.Ohlc.Where(e => e.FrameSpanSeconds == 60)
            .OrderByDescending(e => e.Start)
            .FirstOrDefault()?.Start ?? DateTime.MinValue;

        public void Add(DbManageRecord manageRec) => _ctx.ManageTable.Add(manageRec);

        public void Update(IEnumerable<DbManageRecord> manageRecs)
        {
            _ctx.ManageTable.RemoveRange(_ctx.ManageTable);
            _ctx.ManageTable.AddRange(manageRecs.Cast<DbManageRecord>()); // Cast back
        }

        public void Add(DbExecution exec) => _ctx.Executions.Add(exec);
        public void Add(DbOhlc ohlc) => _ctx.Ohlc.Add(ohlc);

        public void SaveChanges() => _ctx.SaveChanges();
    }
}
