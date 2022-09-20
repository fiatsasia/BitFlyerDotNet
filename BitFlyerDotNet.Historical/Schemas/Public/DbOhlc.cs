//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Historical;

public class DbOhlc
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
    public decimal Volume { get { return BuyVolume + SellVolume + ExecutedVolume; } set { ExecutedVolume = value; } }

    [Column(Order = 6)]
    public decimal BuyVolume { get; set; }

    [Column(Order = 7)]
    public decimal SellVolume { get; set; }

    [Column(Order = 8)]
    public decimal ExecutedVolume { get; set; }

    [Column(Order = 9)]
    public double VWAP { get; set; }

    [Column(Order = 10)]
    public int ExecutionCount { get; set; }

    [Column(Order = 11)]
    public long StartExecutionId { get; set; }

    [Column(Order = 12)]
    public long EndExecutionId { get; set; }

    [Column(Order = 13)]
    public bool ExecutionIdOutOfOrder { get; set; }

    // SQL Server maps TimeSpan to time but SqlDbType.Time supports on less than a day.
    [NotMapped]
    public TimeSpan FrameSpan
    {
        get { return TimeSpan.FromSeconds(FrameSpanSeconds); }
        set { FrameSpanSeconds = Convert.ToInt32(value.TotalSeconds); }
    }

    decimal _amount;

    public DbOhlc()
    {
    }

    public DbOhlc(TimeSpan frameSpan, DbExecution exec)
    {
        FrameSpan = frameSpan;
        Start = exec.ExecutedTime.Round(frameSpan);
        Open = High = Low = Close = exec.Price;
        UpdateSize(exec);
        _amount = exec.Price * exec.Size;
        VWAP = Convert.ToDouble(exec.Price);
        ExecutionCount = 1;
        StartExecutionId = EndExecutionId = exec.ExecutionId;
        ExecutionIdOutOfOrder = false;
    }

    public static DbOhlc CreateMissingFrame(DbOhlc prev)
    {
        return new DbOhlc
        {
            FrameSpan = prev.FrameSpan,
            Start = prev.Start + prev.FrameSpan,
            Open = prev.Close,
            High = prev.Close,
            Low = prev.Close,
            Close = prev.Close,
            BuyVolume = 0,
            SellVolume = 0,
            ExecutedVolume = 0,
            VWAP = prev.VWAP,
            ExecutionCount = 0,
            StartExecutionId = -1,
            EndExecutionId = -1,
            ExecutionIdOutOfOrder = false,
        };
    }

    public void Update(DbExecution exec)
    {
        High = Math.Max(High, exec.Price);
        Low = Math.Min(Low, exec.Price);
        Close = exec.Price;
        UpdateSize(exec);
        _amount += exec.Price * exec.Size;
        if (Volume > 0m)
        {
            VWAP = Convert.ToDouble(_amount / Volume);
        }
        else
        {
            VWAP = Convert.ToDouble((Open + High + Low + Close) / 4.0m);
        }
        ExecutionCount++;
        if (EndExecutionId > exec.ExecutionId)
        {
            ExecutionIdOutOfOrder = true;
        }
        else
        {
            EndExecutionId = exec.ExecutionId;
        }
    }

    void UpdateSize(DbExecution exec)
    {
        switch (exec.Side)
        {
            case BfTradeSide.Buy:
                BuyVolume += exec.Size;
                break;

            case BfTradeSide.Sell:
                SellVolume += exec.Size;
                break;

            default:
                ExecutedVolume += exec.Size;
                break;
        }
    }
}
