﻿//==============================================================================
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
    public class DbManageRecord
    {
        [Required]
        [Column(Order = 0)]
        public DateTime CreatedTime { get; set; }

        [Key]
        [Column(Order = 1)]
        public long StartExecutionId { get; set; }

        [Required]
        [Column(Order = 2)]
        public DateTime StartExecutedTime { get; set; }

        [Required]
        [Column(Order = 3)]
        public long EndExecutionId { get; set; }

        [Required]
        [Column(Order = 4)]
        public DateTime EndExecutedTime { get; set; }

        [Required]
        [Column(Order = 5)]
        public long ExecutionCount { get; set; }

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
            StartExecutionId = long.MaxValue;
            StartExecutedTime = DateTime.MaxValue;
            EndExecutionId = long.MinValue;
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
}
