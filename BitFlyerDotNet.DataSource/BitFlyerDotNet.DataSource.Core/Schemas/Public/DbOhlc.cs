//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.DataSource;

public class DbOhlc
{
    public int FrameSpanSeconds { get; set; }
    public DateTime Start { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get { return BuyVolume + SellVolume + ExecutedVolume; } set { ExecutedVolume = value; } }
    public decimal BuyVolume { get; set; }
    public decimal SellVolume { get; set; }
    public decimal ExecutedVolume { get; set; }
    public double VWAP { get; set; }
    public int ExecutionCount { get; set; }
    public long StartExecutionId { get; set; }
    public long EndExecutionId { get; set; }
    public bool ExecutionIdOutOfOrder { get; set; }

    // SQL Server maps TimeSpan to time but SqlDbType.Time supports on less than a day.
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
