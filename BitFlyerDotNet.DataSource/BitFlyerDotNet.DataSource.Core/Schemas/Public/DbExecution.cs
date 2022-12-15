//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.DataSource;

public class DbExecution
{
    public long ExecutionId { get; set; }
    public decimal Price { get; set; }
    public decimal Size { get; set; }
    public DateTime ExecutedTime { get; set; }
    public string BuySell { get; set; }

    public BfTradeSide Side
    {
        get
        {
            switch (BuySell)
            {
                case "B": return BfTradeSide.Buy;
                case "S": return BfTradeSide.Sell;
                case "E": return BfTradeSide.BuySell;
                default: throw new ArgumentException();
            }
        }
        set
        {
            switch (value)
            {
                case BfTradeSide.Buy: BuySell = "B"; break;
                case BfTradeSide.Sell: BuySell = "S"; break;
                case BfTradeSide.BuySell: BuySell = "E"; break;
                default: throw new ArgumentException();
            }
        }
    }

    public DbExecution()
    {
    }

    public DbExecution(BfExecution exec)
    {
        ExecutionId = exec.Id;
        ExecutedTime = exec.ExecDate;
        Side = exec.Side;
        Price = exec.Price;
        Size = exec.Size;
    }
}
