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
}
