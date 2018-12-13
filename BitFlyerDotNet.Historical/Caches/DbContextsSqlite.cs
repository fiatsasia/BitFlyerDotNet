//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All right reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    abstract class BfDbContextSqliteBase : DbContext
    {
        readonly string _dbFilePath;

        public BfDbContextSqliteBase(DbContextOptions options, BfProductCode productCode, string cacheFolderBasePath, string name)
            : base(options)
        {
            var dbFolderPath = Path.Combine(cacheFolderBasePath, productCode.ToString());
            Directory.CreateDirectory(dbFolderPath);

            _dbFilePath = Path.Combine(dbFolderPath, name + ".db3");
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
            modelBuilder.Entity<DbHistoricalOhlc>().HasKey(c => new { c.FrameSpanSeconds, c.Start });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("data source=" + _dbFilePath);
        }
    }

    class ExecutionsDbSqliteContext : BfDbContextSqliteBase
    {
        public DbSet<DbExecution> Instance { get; set; }

        public ExecutionsDbSqliteContext(BfProductCode productCode, string cacheFolderBasePath, string name)
            : base(new DbContextOptionsBuilder<ExecutionsDbSqliteContext>().Options, productCode, cacheFolderBasePath, name)
        {
        }

        // Entity Framework Core can not create index with [Index] annotation
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbExecution>().HasIndex(b => b.ExecutedTime);
        }
    }

    class ManageSqliteDbContext : BfDbContextSqliteBase
    {
        public DbSet<DbManageRecord> Instance { get; set; }

        public ManageSqliteDbContext(BfProductCode productCode, string cacheFolderBasePath, string name)
            : base(new DbContextOptionsBuilder<ManageSqliteDbContext>().Options, productCode, cacheFolderBasePath, name)
        {
        }
    }

    class MarkerSqliteDbContext : BfDbContextSqliteBase
    {
        public DbSet<DbMinuteMarker> Instance { get; set; }

        public MarkerSqliteDbContext(BfProductCode productCode, string cacheFolderBasePath, string name)
            : base(new DbContextOptionsBuilder<MarkerSqliteDbContext>().Options, productCode, cacheFolderBasePath, name)
        {
        }
    }

    // https://docs.microsoft.com/ja-jp/ef/core/miscellaneous/configuring-dbcontext
    class OhlcSqliteDbContext : BfDbContextSqliteBase
    {
        public DbSet<DbHistoricalOhlc> Instance { get; set; }

        public OhlcSqliteDbContext(BfProductCode productCode, string cacheFolderBasePath, string name)
            : base(new DbContextOptionsBuilder<OhlcSqliteDbContext>().Options, productCode, cacheFolderBasePath, name)
        {
        }
    }

    class SqliteCacheDbContext : ICacheDbContext
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

            _ctxManage = new ManageSqliteDbContext(productCode, cacheFolderBasePath, "MANAGE");
            _ctxExec = new ExecutionsDbSqliteContext(productCode, cacheFolderBasePath, "EXEC");
            _ctxMarker = new MarkerSqliteDbContext(productCode, cacheFolderBasePath, "MARKER");
        }

        public void SaveExecutionChanges()
        {
            _ctxManage.SaveChanges();
            _ctxExec.SaveChanges();
            _ctxMarker.SaveChanges();
        }

        public void ClearCache()
        {
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
        public IEnumerable<IBfExecution> GetBackwardExecutions(int before, int after)
        {
            return _ctxExec.Instance
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
                return new OhlcSqliteDbContext(ProductCode, _cacheFolderBasePath, string.Format("OHLC_{0}", Convert.ToInt32(frameSpan.TotalMinutes)));
            });

            return ctx.Instance;
        }

        public IEnumerable<IBfOhlc> GetOhlcsBackward(TimeSpan frameSpan, DateTime endFrom, TimeSpan span)
        {
            var end = endFrom - span + frameSpan;
            return GetOhlc(frameSpan).Where(e => e.Start <= endFrom && e.Start >= end).OrderByDescending(e => e.Start);
        }

        public void AddOhlc(TimeSpan frameSpan, IBfOhlc ohlc)
        {
            var dbOhlc = default(DbHistoricalOhlc);
            if (ohlc is DbHistoricalOhlc)
            {
                dbOhlc = ohlc as DbHistoricalOhlc;
            }
            else if (ohlc is IBfOhlcEx)
            {
                dbOhlc = new DbHistoricalOhlc(ohlc as IBfOhlcEx, frameSpan);
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
