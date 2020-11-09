//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public class DbPrivateExecution : IBfExecution
    {
        [Key]
        [Column(Order = 0)]
        public int ExecutionId { get; set; }

        //[Key]
        [Required]
        [Column(Order = 1)]
        public BfProductCode ProductCode { get; set; }

        [Required]
        [Column(Order = 2)]
        public string ChildOrderId { get; set; }

        [Required]
        [Column(Order = 3)]
        public BfTradeSide Side { get; set; }

        [Required]
        [Column(Order = 4)]
        public decimal Price { get; set; }

        [Required]
        [Column(Order = 5)]
        public decimal Size { get; set; }

        [Required]
        [Column(Order = 6)]
        public decimal Commission { get; set; }

        //[Key]
        [Required]
        [Column(Order = 7)]
        public DateTime ExecutedTime { get; set; }

        [Required]
        [Column(Order = 8)]
        public string ChildOrderAcceptanceId { get; set; }

        public DbPrivateExecution()
        {
        }

        public DbPrivateExecution(BfProductCode productCode, BfPrivateExecution exec)
        {
            ExecutionId = exec.ExecutionId;
            ProductCode = productCode;
            ChildOrderId = exec.ChildOrderId;
            Side = exec.Side;
            Price = exec.Price;
            Size = exec.Size;
            Commission = exec.Commission;
            ExecutedTime = exec.ExecutedTime;
            ChildOrderAcceptanceId = exec.ChildOrderAcceptanceId;
        }
    }
}
