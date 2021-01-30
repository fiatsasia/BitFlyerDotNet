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

namespace BitFlyerDotNet.Historical
{
    class DbMinuteMarker
    {
        [Key]
        [Column(Order = 0)]
        public DateTime MarkedTime { get; set; }

        [Required]
        [Column(Order = 1)]
        public long StartExecutionId { get; set; }

        [Required]
        [Column(Order = 2)]
        public long EndExecutionId { get; set; }

        [Required]
        [Column(Order = 3)]
        public int ExecutionCount { get; set; }

        public DbMinuteMarker()
        {
        }
    }
}
