//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    abstract class BfDbContextSqliteBase : DbContext
    {
        readonly string _connStr;

        public BfDbContextSqliteBase(DbContextOptions options, string connStr)
            : base(options)
        {
            _connStr = connStr;
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            this.Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var conn = this.Database.GetDbConnection();
            conn.Open();
            using (var command = conn.CreateCommand())
            {
                command.CommandText = "PRAGMA datetimeformatstring=\"yyyy-MM-dd HH:mm:ss.fff\";";
                command.ExecuteNonQuery();
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connStr);
        }
    }

    class ManageSqliteDbContext : BfDbContextSqliteBase
    {
        public DbSet<DbManageRecord> Instance { get; set; }

        public ManageSqliteDbContext(string connStr)
            : base(new DbContextOptionsBuilder<ManageSqliteDbContext>().Options, connStr)
        {
        }
    }

    class ExecutionsDbSqliteContext : BfDbContextSqliteBase
    {
        public DbSet<DbExecution> Instance { get; set; }

        public ExecutionsDbSqliteContext(string connStr)
            : base(new DbContextOptionsBuilder<ExecutionsDbSqliteContext>().Options, connStr)
        {
        }

        // Entity Framework Core can not create index with [Index] annotation
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbExecution>().HasIndex(b => b.ExecutedTime);
        }
    }

    // https://docs.microsoft.com/ja-jp/ef/core/miscellaneous/configuring-dbcontext
    class OhlcSqliteDbContext : BfDbContextSqliteBase
    {
        public DbSet<DbOhlc> Instance { get; set; }

        public OhlcSqliteDbContext(string connStr)
            : base(new DbContextOptionsBuilder<OhlcSqliteDbContext>().Options, connStr)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbOhlc>().HasKey(c => new { c.FrameSpanSeconds, c.Start });
        }
    }

    class SqliteDbContext : ICacheDbContext //, IDisposable
    {
        public IQueryable<DbManageRecord> ManageTable => _ctxManage.Instance.OrderByDescending(e => e.StartExecutionId);
        public IQueryable<DbExecution> Executions => _ctxExec.Instance.AsNoTracking();
        public IQueryable<DbOhlc> GetOhlcs(TimeSpan period)
        {
            var sec = Convert.ToInt32(period.TotalSeconds); // to optimize for query builder
            return _ctxOhlc.Instance.Where(e => e.FrameSpanSeconds == sec).OrderBy(e => e.Start);
        }

        // Parameters
        public BfProductCode _productCode { get; }
        readonly string _cacheFolderBasePath;

        // Privates
        ManageSqliteDbContext _ctxManage;
        ExecutionsDbSqliteContext _ctxExec;
        OhlcSqliteDbContext _ctxOhlc;

        public SqliteDbContext(string cacheFolderBasePath, BfProductCode productCode)
        {
            _cacheFolderBasePath = cacheFolderBasePath;
            _productCode = productCode;
            CreateContext();
        }

        void CreateContext()
        {
            var dbFolderPath = Path.Combine(_cacheFolderBasePath, _productCode.ToString());
            Directory.CreateDirectory(dbFolderPath);

            _ctxManage = new ManageSqliteDbContext("data source=" + Path.Combine(dbFolderPath, "MANAGE.db3"));
            _ctxExec = new ExecutionsDbSqliteContext("data source=" + Path.Combine(dbFolderPath, "EXEC.db3"));
            _ctxOhlc = new OhlcSqliteDbContext("data source=" + Path.Combine(dbFolderPath, "OHLC.db3"));
        }

        public void Dispose()
        {
            _ctxManage.Dispose();
            _ctxExec.Dispose();
        }

        public void SaveChanges()
        {
            _ctxManage.SaveChanges();
            _ctxExec.SaveChanges();
            _ctxOhlc.SaveChanges();
        }

        public void ClearCache()
        {
            _ctxManage.Dispose();
            _ctxExec.Dispose();
            CreateContext();
        }

        public DateTime LastExecutionTime => ManageTable.First().EndExecutedTime;
        public DateTime LastOhlcTime => _ctxOhlc.Instance.Where(e => e.FrameSpanSeconds == 60)
            .OrderByDescending(e => e.Start)
            .FirstOrDefault()?.Start ?? DateTime.MinValue;

        public void Update(IEnumerable<DbManageRecord> manageRecs)
        {
            _ctxManage.Instance.RemoveRange(_ctxManage.Instance);
            _ctxManage.Instance.AddRange(manageRecs);
        }

        public void Add(DbManageRecord manageRec) => _ctxManage.Instance.Add(manageRec);
        public void Add(DbOhlc ohlc) => _ctxOhlc.Instance.Add(ohlc);

        public void Add(DbExecution exec) => _ctxExec.Instance.Add(exec);
    }
}
