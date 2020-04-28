//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Financial.Extensions;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public interface IManageRecord
    {
        DateTime CreatedTime { get; set; }
        int StartExecutionId { get; set; }
        DateTime StartExecutedTime { get; set; }
        int EndExecutionId { get; set; }
        DateTime EndExecutedTime { get; set; }
        int ExecutionCount { get; set; }
        string TransactionKind { get; set; }
        DateTime LastUpdatedTime { get; set; }
    }

    class DbManageRecord : IManageRecord
    {
        [Required]
        [Column(Order = 0)]
        public DateTime CreatedTime { get; set; }

        [Key]
        [Column(Order = 1)]
        public int StartExecutionId { get; set; }

        [Required]
        [Column(Order = 2)]
        public DateTime StartExecutedTime { get; set; }

        [Required]
        [Column(Order = 3)]
        public int EndExecutionId { get; set; }

        [Required]
        [Column(Order = 4)]
        public DateTime EndExecutedTime { get; set; }

        [Required]
        [Column(Order = 5)]
        public int ExecutionCount { get; set; }

        [Required]
        [Column(Order = 6)]
        public string TransactionKind { get; set; }

        [Required]
        [Column(Order = 7)]
        public DateTime LastUpdatedTime { get; set; }

        public DbManageRecord()
        {
            ExecutionCount = 0;
            CreatedTime = LastUpdatedTime = DateTime.UtcNow;
            StartExecutionId = int.MaxValue;
            StartExecutedTime = DateTime.MaxValue;
            EndExecutionId = int.MinValue;
            TransactionKind = "H";
            EndExecutedTime = DateTime.MinValue;
        }

        public void Update(IBfExecution exec)
        {
            StartExecutionId = Math.Min(exec.ExecutionId, StartExecutionId);
            StartExecutedTime = (exec.ExecutedTime < StartExecutedTime) ? exec.ExecutedTime : StartExecutedTime;
            EndExecutionId = Math.Max(exec.ExecutionId, EndExecutionId);
            EndExecutedTime = (exec.ExecutedTime > EndExecutedTime) ? exec.ExecutedTime : EndExecutedTime;
            LastUpdatedTime = DateTime.UtcNow;
            ExecutionCount++;
        }
    }

    class DbExecution : IBfExecution
    {
        [Key]
        [Column(Order = 0)]
        public int ExecutionId { get; set; }

        [Required]
        [Column(Order = 1)]
        public decimal Price { get; set; }

        [Required]
        [Column(Order = 2)]
        public decimal Size { get; set; }

        [Required]
        [Column(Order = 3)]
        public DateTime ExecutedTime { get; set; }

        [Required]
        [Column(Order = 4)]
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

        public DbExecution()
        {
        }

        public DbExecution(IBfExecution exec)
        {
            ExecutionId = exec.ExecutionId;
            ExecutedTime = exec.ExecutedTime;
            Side = exec.Side;
            Price = exec.Price;
            Size = exec.Size;
        }
    }

    class DbMinuteMarker
    {
        [Key]
        [Column(Order = 0)]
        public DateTime MarkedTime { get; set; }

        [Required]
        [Column(Order = 1)]
        public int StartExecutionId { get; set; }

        [Required]
        [Column(Order = 2)]
        public int EndExecutionId { get; set; }

        [Required]
        [Column(Order = 3)]
        public int ExecutionCount { get; set; }

        public DbMinuteMarker()
        {
        }
    }

    class DbHistoricalOhlc : IOhlcvv<decimal>
    {
        [Key]
        [Column(Order = 0)]
        public int FrameSpanSeconds { get; set; }

        [Key]
        [Column(Order = 1)]
        public DateTime Start { get; set; }

        [Column(Order = 2)]
        public decimal Open { get; set; }

        [Column(Order = 3)]
        public decimal High { get; set; }

        [Column(Order = 4)]
        public decimal Low { get; set; }

        [Column(Order = 5)]
        public decimal Close { get; set; }

        [NotMapped]
        public double Volume { get { return BuyVolume + SellVolume + ExecutedVolume; } set { ExecutedVolume = value; } }

        [Column(Order = 6)]
        public int ExecutionCount { get; set; }

        [Column(Order = 7)]
        public double VWAP { get; set; }

        [Column(Order = 8)]
        public double BuyVolume { get; set; }

        [Column(Order = 9)]
        public double SellVolume { get; set; }

        [Column(Order = 10)]
        public double ExecutedVolume { get; set; }

        // SQL Server maps TimeSpan to time but SqlDbType.Time supports on less than a day.
        [NotMapped]
        public TimeSpan FrameSpan
        {
            get { return TimeSpan.FromSeconds(FrameSpanSeconds); }
            set { FrameSpanSeconds = Convert.ToInt16(value.TotalSeconds); }
        }

        public Type GetBaseType()
        {
            return ExecutionCount == 0 ? typeof(IOhlcvv<decimal>) : typeof(IBfOhlc);
        }

        public DbHistoricalOhlc()
        {
        }

        public DbHistoricalOhlc(IOhlcvv<decimal> ohlc, TimeSpan frameSpan)
        {
            Start = ohlc.Start;
            Open = ohlc.Open;
            High = ohlc.High;
            Low = ohlc.Low;
            Close = ohlc.Close;
            Volume = ohlc.Volume;
            VWAP = ohlc.VWAP;
            FrameSpan = frameSpan;
        }

        public DbHistoricalOhlc(IBfOhlc ohlc, TimeSpan frameSpan)
        {
            Start = ohlc.Start;
            Open = ohlc.Open;
            High = ohlc.High;
            Low = ohlc.Low;
            Close = ohlc.Close;
            ExecutionCount = ohlc.ExecutionCount;
            VWAP = ohlc.VWAP;
            BuyVolume = ohlc.BuyVolume;
            SellVolume = ohlc.SellVolume;
            ExecutedVolume = ohlc.ExecutedVolume;
            FrameSpan = frameSpan;
        }
    }
}
