//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitFlyerDotNet.Historical
{
    class DbHistoricalOhlc : IOhlcvv
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
            return ExecutionCount == 0 ? typeof(IOhlcvv) : typeof(IBfOhlc);
        }

        public DbHistoricalOhlc()
        {
        }

        public DbHistoricalOhlc(IOhlcvv ohlc, TimeSpan frameSpan)
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
