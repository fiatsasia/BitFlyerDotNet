//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All right reserved.
// https://www.fiats.asia/
//

using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public class DbExecutionTickRow : IBfExecution
    {
#if false
        internal static readonly string CreateTableQuery =
            @"CREATE TABLE {0} (
                ExecutionId     INTEGER  NOT NULL PRIMARY KEY DESC,
                Price           REAL     NOT NULL,
                Size            REAL     NOT NULL,
                ExecutedTime    DATETIME NOT NULL,
                BuySell         CHAR (1) NOT NULL
            );
            CREATE INDEX ExecutedTimeIndex ON {0}(ExecutedTime ASC);
            ";
#endif

        [Key]
        [Column(Order = 1)]
        public int ExecutionId { get; set; }

        [Required]
        [Column(Order = 2)]
        public double Price { get; set; }

        [Required]
        [Column(Order = 3)]
        public double Size { get; set; }

        [Required]
        [Column(Order = 4)]
        public DateTime ExecutedTime { get; set; }

        [Required]
        [Column(Order = 5)]
        public string BuySell { get; set; }

        [NotMapped]
        public BfTradeSide Side
        {
            get
            {
                switch (BuySell)
                {
                    case "B": return BfTradeSide.Buy;
                    case "S": return BfTradeSide.Sell;
                    case "E": return BfTradeSide.Unknown;
                    default: throw new ArgumentException();
                }
            }
            set
            {
                switch (value)
                {
                    case BfTradeSide.Buy: BuySell = "B"; break;
                    case BfTradeSide.Sell: BuySell = "S"; break;
                    case BfTradeSide.Unknown: BuySell = "E"; break;
                    default: throw new ArgumentException();
                }
            }
        }

        [NotMapped]
        public string ChildOrderAcceptanceId { get { return string.Empty; } }

        public DbExecutionTickRow()
        {
        }

        public DbExecutionTickRow(IBfExecution tick)
        {
            ExecutionId = tick.ExecutionId;
            ExecutedTime = tick.ExecutedTime;
            Side = tick.Side;
            Price = tick.Price;
            Size = tick.Size;
        }
    }

    public class ExecutionDbContext : BfDbContextBase
    {
        public DbSet<DbExecutionTickRow> Instance { get; set; }

        public ExecutionDbContext(BfProductCode productCode, string cacheFolderBasePath, string name)
            : base(new DbContextOptionsBuilder<ExecutionDbContext>().Options, productCode, cacheFolderBasePath, name)
        {
        }

        // Entity Framework Core can not create index with [Index] annotation
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbExecutionTickRow>().HasIndex(b => b.ExecutedTime);
        }
    }


    public class DbExecutionBlocksRow
    {
#if false
        internal static readonly string CreateTableQuery =
            @"CREATE TABLE {0} (
                CreatedTime     DATETIME NOT NULL,
                StartTickId     INTEGER  NOT NULL PRIMARY KEY DESC,
                StartTickTime   DATETIME NOT NULL,
                EndTickId       INTEGER  NOT NULL,
                EndTickTime     DATETIME NOT NULL,
                Ticks           INTEGER  NOT NULL,
                TransactionKind CHAR (1) NOT NULL,
                LastUpdatedTime DATETIME NOT NULL
            );";
#endif

        [Required]
        [Column(Order = 1)]
        public DateTime CreatedTime { get; set; }

        [Key]
        [Column(Order = 2)]
        public Int32 StartTickId { get; set; }

        [Required]
        [Column(Order = 3)]
        public DateTime StartTickTime { get; set; }

        [Required]
        [Column(Order = 4)]
        public int EndTickId { get; set; }

        [Required]
        [Column(Order = 5)]
        public DateTime EndTickTime { get; set; }

        [Required]
        [Column(Order = 6)]
        public Int32 Ticks { get; set; }

        [Required]
        [Column(Order = 7)]
        public string TransactionKind { get; set; }

        [Required]
        [Column(Order = 8)]
        public DateTime LastUpdatedTime { get; set; }

        public DbExecutionBlocksRow()
        {
            Ticks = 0;
            CreatedTime = LastUpdatedTime = DateTime.UtcNow;
            StartTickId = int.MaxValue;
            StartTickTime = DateTime.MaxValue;
            EndTickId = int.MinValue;
            TransactionKind = "H";
            EndTickTime = DateTime.MinValue;
        }

        public DbExecutionBlocksRow(DbExecutionBlocksRow row)
        {
            Update(row);
        }

        public void Update(IBfExecution tick)
        {
            StartTickId = Math.Min(tick.ExecutionId, StartTickId);
            StartTickTime = (tick.ExecutedTime < StartTickTime) ? tick.ExecutedTime : StartTickTime;
            EndTickId = Math.Max(tick.ExecutionId, EndTickId);
            EndTickTime = (tick.ExecutedTime > EndTickTime) ? tick.ExecutedTime : EndTickTime;
            LastUpdatedTime = DateTime.UtcNow;
            Ticks++;
        }

        public void Update(DbExecutionBlocksRow row)
        {
            CreatedTime = row.CreatedTime;
            StartTickId = row.StartTickId;
            StartTickTime = row.StartTickTime;
            EndTickId = row.EndTickId;
            EndTickTime = row.EndTickTime;
            Ticks = row.Ticks;
            TransactionKind = row.TransactionKind;
            LastUpdatedTime = row.LastUpdatedTime;
        }
    }

    public class ExecutionBlockDbContext : BfDbContextBase
    {
        public DbSet<DbExecutionBlocksRow> Instance { get; set; }

        public ExecutionBlockDbContext(BfProductCode productCode, string cacheFolderBasePath, string name)
            : base(new DbContextOptionsBuilder<ExecutionBlockDbContext>().Options, productCode, cacheFolderBasePath, name)
        {
        }
    }

    public class ExecutionMinuteMarkerRow
    {
#if false
        internal static readonly string CreateTableQuery =
            @"CREATE TABLE {0} (
                MarkedTime  DATETIME NOT NULL PRIMARY KEY DESC,
                StartTickId INTEGER  NOT NULL,
                EndTickId   INTEGER  NOT NULL,
                TickCount   INTEGER  NOT NULL
            );";
#endif

        [Key]
        [Column(Order = 1)]
        public DateTime MarkedTime { get; set; }

        [Required]
        [Column(Order = 2)]
        public int StartTickId { get; set; }

        [Required]
        [Column(Order = 3)]
        public int EndTickId { get; set; }

        [Required]
        [Column(Order = 4)]
        public int TickCount { get; set; }

        public ExecutionMinuteMarkerRow()
        {
        }
    }

    public class ExecutionMinuteMarketDbContext : BfDbContextBase
    {
        public DbSet<ExecutionMinuteMarkerRow> Instance { get; set; }

        public ExecutionMinuteMarketDbContext(BfProductCode productCode, string cacheFolderBasePath, string name)
            : base(new DbContextOptionsBuilder<ExecutionMinuteMarketDbContext>().Options, productCode, cacheFolderBasePath, name)
        {
        }
    }

    public class BfHistoricalOhlc : IBfOhlc
    {
#if false
        internal const string CreateTableQuery =
            @"CREATE TABLE {0} (
            Start   DATETIME    NOT NULL PRIMARY KEY DESC,
            Open    REAL        NOT NULL,
            High    REAL        NOT NULL,
            Low     REAL        NOT NULL,
            Close   REAL        NOT NULL,
            Volume  REAL        NOT NULL);";
#endif

        [Key]
        [Column(Order = 1)]
        public DateTime Start { get; set; }

        [Column(Order = 2)]
        public double Open { get; set; }

        [Column(Order = 3)]
        public double High { get; set; }

        [Column(Order = 4)]
        public double Low { get; set; }

        [Column(Order = 5)]
        public double Close { get; set; }

        [Column(Order = 6)]
        public double Volume { get; set; }

        public BfHistoricalOhlc()
        {
        }
    }

    // https://docs.microsoft.com/ja-jp/ef/core/miscellaneous/configuring-dbcontext
    public class HistoricalDbContext : BfDbContextBase
    {
        public DbSet<BfHistoricalOhlc> Ohlcs { get; set; }

        public HistoricalDbContext(BfProductCode productCode, string cacheFolderBasePath, string name)
            : base(new DbContextOptionsBuilder<HistoricalDbContext>().Options, productCode, cacheFolderBasePath, name)
        {
        }
    }

    public abstract class BfDbContextBase : DbContext
    {
        string _dbFilePath;

        public BfDbContextBase(DbContextOptions options, BfProductCode productCode, string cacheFolderBasePath, string name)
            : base(options)
        {
            var dbFolderPath = Path.Combine(cacheFolderBasePath, productCode.ToString());
            Directory.CreateDirectory(dbFolderPath);

            _dbFilePath = Path.Combine(dbFolderPath, name + ".db3");
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            this.Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("data source=" + _dbFilePath);
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
    }
}
