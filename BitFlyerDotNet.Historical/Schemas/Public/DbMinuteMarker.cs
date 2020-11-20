﻿//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitFlyerDotNet.Historical
{
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
}