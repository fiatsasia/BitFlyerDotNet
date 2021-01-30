//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    class DbExecution : IBfExecution
    {
        [Key]
        [Column(Order = 0)]
        public long ExecutionId { get; set; }

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
}
