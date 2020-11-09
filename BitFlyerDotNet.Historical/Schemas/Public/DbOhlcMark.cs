//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitFlyerDotNet.Historical
{
    class DbOhlcMark
    {
        [Key]
        [Column(Order = 0)]
        public DateTime StartTime { get; set; }

        [Column(Order = 1)]
        public decimal OpenPrice { get; set; }

        [Column(Order = 2)]
        public decimal HighPrice { get; set; }

        [Column(Order = 3)]
        public decimal LowPrice { get; set; }

        [Column(Order = 4)]
        public decimal ClosePrice { get; set; }

        [Column(Order = 5)]
        public decimal Volume { get; set; }

        [Column(Order = 6)]
        public decimal VWAP { get; set; }

        [Column(Order = 7)]
        public int StartExecutionId { get; set; }

        [Column(Order = 8)]
        public int EndExecutionId { get; set; }
    }
}
