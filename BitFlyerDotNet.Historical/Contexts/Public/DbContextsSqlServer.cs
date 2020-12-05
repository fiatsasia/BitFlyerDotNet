//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    abstract class BfDbContextBaseSqlServer : DbContext
    {
        readonly string _connStr;
        static readonly TimeSpan CommandTimeout = TimeSpan.FromHours(1);

        public abstract DbSet<DbManageRecord> GetManageTable();
        public abstract DbSet<DbExecution> GetExecutions();
        public abstract DbSet<DbMinuteMarker> GetMarker();
        public abstract DbSet<DbHistoricalOhlc> GetOhlc();
        public abstract DbSet<DbOhlcMark> GetOhlcMark();

        public BfDbContextBaseSqlServer(string connStr, DbContextOptions options)
            : base(options)
        {
            _connStr = connStr;
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            Database.SetCommandTimeout(CommandTimeout);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbHistoricalOhlc>().HasKey(c => new { c.FrameSpanSeconds, c.Start });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connStr);
        }
    }

    // FXBTCJPY
    class BfFxBtcJpyDbContextSqlServer : BfDbContextBaseSqlServer
    {
        public DbSet<DbManageRecord> FXBTCJPY_ManageTable { get; set; }
        public DbSet<DbExecution> FXBTCJPY_Executions { get; set; }
        public DbSet<DbMinuteMarker> FXBTCJPY_Marker { get; set; }
        public DbSet<DbHistoricalOhlc> FXBTCJPY_Ohlc { get; set; }
        public DbSet<DbOhlcMark> FXBTCJPY_OHLC_Mark { get; set; }

        public override DbSet<DbManageRecord> GetManageTable() { return FXBTCJPY_ManageTable; }
        public override DbSet<DbExecution> GetExecutions() { return FXBTCJPY_Executions; }
        public override DbSet<DbMinuteMarker> GetMarker() { return FXBTCJPY_Marker; }
        public override DbSet<DbHistoricalOhlc> GetOhlc() { return FXBTCJPY_Ohlc; }
        public override DbSet<DbOhlcMark> GetOhlcMark() { return FXBTCJPY_OHLC_Mark; }

        public BfFxBtcJpyDbContextSqlServer(string connStr)
            : base(connStr, new DbContextOptionsBuilder<BfFxBtcJpyDbContextSqlServer>().Options) { }
    }

    // BTCJPY
    class BfBtcJpyDbContextSqlServer : BfDbContextBaseSqlServer
    {
        public DbSet<DbManageRecord> BTCJPY_ManageTable { get; set; }
        public DbSet<DbExecution> BTCJPY_Executions { get; set; }
        public DbSet<DbMinuteMarker> BTCJPY_Marker { get; set; }
        public DbSet<DbHistoricalOhlc> BTCJPY_Ohlc { get; set; }
        public DbSet<DbOhlcMark> BTCJPY_OHLC_Mark { get; set; }

        public override DbSet<DbManageRecord> GetManageTable() { return BTCJPY_ManageTable; }
        public override DbSet<DbExecution> GetExecutions() { return BTCJPY_Executions; }
        public override DbSet<DbMinuteMarker> GetMarker() { return BTCJPY_Marker; }
        public override DbSet<DbHistoricalOhlc> GetOhlc() { return BTCJPY_Ohlc; }
        public override DbSet<DbOhlcMark> GetOhlcMark() { return BTCJPY_OHLC_Mark; }

        public BfBtcJpyDbContextSqlServer(string connStr)
            : base(connStr, new DbContextOptionsBuilder<BfBtcJpyDbContextSqlServer>().Options) { }
    }

    // ETHBTC
    class BfEthBtcDbContextSqlServer : BfDbContextBaseSqlServer
    {
        public DbSet<DbManageRecord> ETHBTC_ManageTable { get; set; }
        public DbSet<DbExecution> ETHBTC_Executions { get; set; }
        public DbSet<DbMinuteMarker> ETHBTC_Marker { get; set; }
        public DbSet<DbHistoricalOhlc> ETHBTC_Ohlc { get; set; }
        public DbSet<DbOhlcMark> ETHBTC_OHLC_Mark { get; set; }

        public override DbSet<DbManageRecord> GetManageTable() { return ETHBTC_ManageTable; }
        public override DbSet<DbExecution> GetExecutions() { return ETHBTC_Executions; }
        public override DbSet<DbMinuteMarker> GetMarker() { return ETHBTC_Marker; }
        public override DbSet<DbHistoricalOhlc> GetOhlc() { return ETHBTC_Ohlc; }
        public override DbSet<DbOhlcMark> GetOhlcMark() { return ETHBTC_OHLC_Mark; }

        public BfEthBtcDbContextSqlServer(string connStr)
            : base(connStr, new DbContextOptionsBuilder<BfEthBtcDbContextSqlServer>().Options) { }
    }

    // BCHBTC
    class BfBCHBtcDbContextSqlServer : BfDbContextBaseSqlServer
    {
        public DbSet<DbManageRecord> BCHBTC_ManageTable { get; set; }
        public DbSet<DbExecution> BCHBTC_Executions { get; set; }
        public DbSet<DbMinuteMarker> BCHBTC_Marker { get; set; }
        public DbSet<DbHistoricalOhlc> BCHBTC_Ohlc { get; set; }
        public DbSet<DbOhlcMark> BCHBTC_OHLC_Mark { get; set; }

        public override DbSet<DbManageRecord> GetManageTable() { return BCHBTC_ManageTable; }
        public override DbSet<DbExecution> GetExecutions() { return BCHBTC_Executions; }
        public override DbSet<DbMinuteMarker> GetMarker() { return BCHBTC_Marker; }
        public override DbSet<DbHistoricalOhlc> GetOhlc() { return BCHBTC_Ohlc; }
        public override DbSet<DbOhlcMark> GetOhlcMark() { return BCHBTC_OHLC_Mark; }

        public BfBCHBtcDbContextSqlServer(string connStr)
            : base(connStr, new DbContextOptionsBuilder<BfBCHBtcDbContextSqlServer>().Options) { }
    }

    // ETHJPY
    class BfEthJpyDbContextSqlServer : BfDbContextBaseSqlServer
    {
        public DbSet<DbManageRecord> ETHJPY_ManageTable { get; set; }
        public DbSet<DbExecution> ETHJPY_Executions { get; set; }
        public DbSet<DbMinuteMarker> ETHJPY_Marker { get; set; }
        public DbSet<DbHistoricalOhlc> ETHJPY_Ohlc { get; set; }
        public DbSet<DbOhlcMark> ETHJPY_OHLC_Mark { get; set; }

        public override DbSet<DbManageRecord> GetManageTable() { return ETHJPY_ManageTable; }
        public override DbSet<DbExecution> GetExecutions() { return ETHJPY_Executions; }
        public override DbSet<DbMinuteMarker> GetMarker() { return ETHJPY_Marker; }
        public override DbSet<DbHistoricalOhlc> GetOhlc() { return ETHJPY_Ohlc; }
        public override DbSet<DbOhlcMark> GetOhlcMark() { return ETHJPY_OHLC_Mark; }

        public BfEthJpyDbContextSqlServer(string connStr)
            : base(connStr, new DbContextOptionsBuilder<BfEthJpyDbContextSqlServer>().Options) { }
    }

    class SqlServerCacheDbContext : ICacheDbContext
    {
        public BfProductCode ProductCode { get; }
        string _connStr;

        BfDbContextBaseSqlServer _ctx;

        public SqlServerCacheDbContext(string connStr, BfProductCode productCode)
        {
            _connStr = connStr;
            ProductCode = productCode;
            CreateContext();
        }

        void CreateContext()
        {
            switch (ProductCode)
            {
                case BfProductCode.FXBTCJPY:
                    _ctx = new BfFxBtcJpyDbContextSqlServer(_connStr);
                    break;

                case BfProductCode.BTCJPY:
                    _ctx = new BfBtcJpyDbContextSqlServer(_connStr);
                    break;

                case BfProductCode.ETHBTC:
                    _ctx = new BfEthBtcDbContextSqlServer(_connStr);
                    break;

                case BfProductCode.BCHBTC:
                    _ctx = new BfBCHBtcDbContextSqlServer(_connStr);
                    break;

                case BfProductCode.ETHJPY:
                    _ctx = new BfEthJpyDbContextSqlServer(_connStr);
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        public void Dispose()
        {
            _ctx.Dispose();
        }

        public void SaveExecutionChanges()
        {
            try
            {
                _ctx.SaveChanges();
            }
            catch (ObjectDisposedException)
            {
                // Skip exceptionb
            }
        }

        public void ClearCache()
        {
            _ctx.Dispose();
            CreateContext();
        }

        //
        // Manage table
        //
        public List<IManageRecord> GetManageTable()
        {
            return _ctx.GetManageTable().AsQueryable().OrderByDescending(e => e.StartExecutionId).Cast<IManageRecord>().ToList();
        }

        public void AddManageRecord(IManageRecord manageRec)
        {
            _ctx.GetManageTable().Add(manageRec as DbManageRecord);
        }

        public void UpdateManageTable(IEnumerable<IManageRecord> manageRecs)
        {
            _ctx.GetManageTable().RemoveRange(_ctx.GetManageTable());
            _ctx.GetManageTable().AddRange(manageRecs.Cast<DbManageRecord>()); // Cast back
        }

        //
        // Executions
        //
        public IEnumerable<IBfExecution> GetBackwardExecutions()
        {
            return _ctx.GetExecutions();
        }

        public IEnumerable<IBfExecution> GetBackwardExecutions(int before, int after)
        {
            return _ctx.GetExecutions().AsQueryable()
                .Where(exec => exec.ExecutionId < before && exec.ExecutionId > after)
                .OrderByDescending(exec => exec.ExecutedTime)
                .ThenByDescending(exec => exec.ExecutionId);
        }

        public void AddExecution(IBfExecution exec)
        {
            _ctx.GetExecutions().Add(exec as DbExecution);
        }

        //
        // OHLCs
        //
        public IEnumerable<IOhlcvv> GetOhlcsBackward(TimeSpan frameSpan, DateTime endFrom, TimeSpan span)
        {
            var end = endFrom - span + frameSpan;
            return _ctx.GetOhlc().AsQueryable().Where(e => e.FrameSpan == frameSpan && e.Start <= endFrom && e.Start >= end).OrderByDescending(e => e.Start);
        }

        public void AddOhlc(TimeSpan frameSpan, IOhlcvv ohlc)
        {
            var dbOhlc = default(DbHistoricalOhlc);
            if (ohlc is DbHistoricalOhlc)
            {
                dbOhlc = ohlc as DbHistoricalOhlc;
            }
            else
            {
                dbOhlc = new DbHistoricalOhlc(ohlc, frameSpan);
            }
            var dbs = _ctx.GetOhlc();
            if (!dbs.Any(e => e.Start == dbOhlc.Start))
            {
                dbs.Add(dbOhlc);
            }
        }

        public void SaveOhlcChanges()
        {
            _ctx.SaveChanges();
        }

        //
        // Marker
        //
        public DbSet<DbMinuteMarker> Marker { get { return _ctx.GetMarker(); } }
    }
}
