//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Financier;
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
#if false
            var conn = _dbctx.Database.GetDbConnection();
            conn.Open();
            using (var command = conn.CreateCommand())
            {
                command.CommandText = "PRAGMA datetimeformatstring=\"yyyy-MM-dd HH:mm:ss.fff\";";
                command.ExecuteNonQuery();
            }
#endif
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

    class MarkerSqliteDbContext : BfDbContextSqliteBase
    {
        public DbSet<DbMinuteMarker> Instance { get; set; }

        public MarkerSqliteDbContext(string connStr)
            : base(new DbContextOptionsBuilder<MarkerSqliteDbContext>().Options, connStr)
        {
        }
    }

    // https://docs.microsoft.com/ja-jp/ef/core/miscellaneous/configuring-dbcontext
    class OhlcSqliteDbContext : BfDbContextSqliteBase
    {
        public DbSet<DbHistoricalOhlc> Instance { get; set; }

        public OhlcSqliteDbContext(string connStr)
            : base(new DbContextOptionsBuilder<OhlcSqliteDbContext>().Options, connStr)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbHistoricalOhlc>().HasKey(c => new { c.FrameSpanSeconds, c.Start });
        }
    }

    class SqliteCacheDbContext : ICacheDbContext //, IDisposable
    {
        // Parameters
        public BfProductCode ProductCode { get; }
        readonly string _cacheFolderBasePath;

        // Privates
        ManageSqliteDbContext _ctxManage;
        ExecutionsDbSqliteContext _ctxExec;
        MarkerSqliteDbContext _ctxMarker;

        public SqliteCacheDbContext(string cacheFolderBasePath, BfProductCode productCode)
        {
            _cacheFolderBasePath = cacheFolderBasePath;
            ProductCode = productCode;
            CreateContext();
        }

        void CreateContext()
        {
            var dbFolderPath = Path.Combine(_cacheFolderBasePath, ProductCode.ToString());
            Directory.CreateDirectory(dbFolderPath);

            _ctxManage = new ManageSqliteDbContext("data source=" + Path.Combine(dbFolderPath, "MANAGE.db3"));
            _ctxExec = new ExecutionsDbSqliteContext("data source=" + Path.Combine(dbFolderPath, "EXEC.db3"));
            _ctxMarker = new MarkerSqliteDbContext("data source=" + Path.Combine(dbFolderPath, "MARKER.db3"));
        }

        public void Dispose()
        {
            _ctxManage.Dispose();
            _ctxExec.Dispose();
            _ctxMarker.Dispose();
        }

        public void SaveExecutionChanges()
        {
            _ctxManage.SaveChanges();
            _ctxExec.SaveChanges();
            _ctxMarker.SaveChanges();
        }

        public void ClearCache()
        {
            _ctxManage.Dispose();
            _ctxExec.Dispose();
            _ctxMarker.Dispose();
            CreateContext();
        }

        //
        // Manage info
        //
        public List<IManageRecord> GetManageTable()
        {
            return _ctxManage.Instance.OrderByDescending(e => e.StartExecutionId).Cast<IManageRecord>().ToList();
        }

        public void UpdateManageTable(IEnumerable<IManageRecord> manageRecs)
        {
            _ctxManage.Instance.RemoveRange(_ctxManage.Instance);
            _ctxManage.Instance.AddRange(manageRecs.Cast<DbManageRecord>()); // Cast back
        }

        public void AddManageRecord(IManageRecord manageRec)
        {
            _ctxManage.Instance.Add(manageRec as DbManageRecord);
        }

        //
        // Executions
        //
        public IEnumerable<IBfExecution> GetBackwardExecutions()
        {
            return _ctxExec.Instance.AsNoTracking();
        }

        public IEnumerable<IBfExecution> GetBackwardExecutions(int before, int after)
        {
            return _ctxExec.Instance.AsNoTracking()
                .Where(exec => exec.ExecutionId < before && exec.ExecutionId > after)
                .OrderByDescending(exec => exec.ExecutedTime)
                .ThenByDescending(exec => exec.ExecutionId);
        }

        public void AddExecution(IBfExecution exec)
        {
            _ctxExec.Instance.Add(exec as DbExecution);
        }

        //
        // OHLCs
        //
        ConcurrentDictionary<TimeSpan, OhlcSqliteDbContext> _ohlcs = new ConcurrentDictionary<TimeSpan, OhlcSqliteDbContext>();
        DbSet<DbHistoricalOhlc> GetOhlc(TimeSpan frameSpan)
        {
            if (!CryptowatchOhlcSource.IsSupportedFrameSpan(frameSpan))
            {
                throw new NotSupportedException("frame span is not supported.");
            }

            var ctx = _ohlcs.GetOrAdd(frameSpan, _ =>
            {
                var dbFolderPath = Path.Combine(_cacheFolderBasePath, ProductCode.ToString());
                Directory.CreateDirectory(dbFolderPath);
                var dbFileName = string.Format("OHLC_{0}.db3", Convert.ToInt32(frameSpan.TotalMinutes));
                return new OhlcSqliteDbContext("data source=" + Path.Combine(dbFolderPath, dbFileName));
            });

            return ctx.Instance;
        }

        public IEnumerable<IOhlcvv<decimal>> GetOhlcsBackward(TimeSpan frameSpan, DateTime endFrom, TimeSpan span)
        {
            var end = endFrom - span + frameSpan;
            return GetOhlc(frameSpan).AsNoTracking().Where(e => e.Start <= endFrom && e.Start >= end).OrderByDescending(e => e.Start);
        }

        public void AddOhlc(TimeSpan frameSpan, IOhlcvv<decimal> ohlc)
        {
            var dbOhlc = default(DbHistoricalOhlc);
            if (ohlc is DbHistoricalOhlc)
            {
                dbOhlc = ohlc as DbHistoricalOhlc;
            }
            else if (ohlc is IBfOhlc)
            {
                dbOhlc = new DbHistoricalOhlc(ohlc as IBfOhlc, frameSpan);
            }
            else
            {
                dbOhlc = new DbHistoricalOhlc(ohlc, frameSpan);
            }
            var dbs = GetOhlc(dbOhlc.FrameSpan);
            if (!dbs.Any(e => e.Start == dbOhlc.Start))
            {
                dbs.Add(dbOhlc);
            }
        }

        public void SaveOhlcChanges()
        {
            _ohlcs.Values.ForEach(e => e.SaveChanges());
        }

        //
        // Marker
        //
        public DbSet<DbMinuteMarker> Marker { get { return _ctxMarker.Instance; } }
    }
}
