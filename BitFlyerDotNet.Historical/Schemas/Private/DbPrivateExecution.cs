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
    public class DbPrivateExecution : IBfPrivateExecution
    {
        [Required]
        [Column(Order = 0)]
        public BfProductCode ProductCode { get; set; }

        [Required]
        [Column(Order = 1)]
        public int ExecutionId { get; set; }

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
        public decimal Amount{ get; set; }

        [Required]
        [Column(Order = 7)]
        public decimal? Commission { get; set; }

        [Required]
        [Column(Order = 8)]
        public DateTime ExecutedTime { get; set; }

        [Required]
        [Column(Order = 9)]
        public string ChildOrderAcceptanceId { get; set; }

        [Column(Order = 10)]
        public decimal? SwapForDifference { get; set; }

        public DbPrivateExecution()
        {
        }

        public DbPrivateExecution(BfProductCode productCode, BfaPrivateExecution exec)
        {
            ProductCode = productCode;
            ExecutionId = exec.ExecutionId;
            Side = exec.Side;
            Price = exec.Price;
            Size = exec.Size;
            if (exec.Side == BfTradeSide.Sell)
            {
                Amount = (exec.Price * exec.Size).Truncate(productCode.GetPriceDecimals());
            }
            else
            {
                Amount = (exec.Price * exec.Size).Ceiling(productCode.GetPriceDecimals());
            }

            ChildOrderId = exec.ChildOrderId;
            ChildOrderAcceptanceId = exec.ChildOrderAcceptanceId;

            Commission = exec.Commission;
            ExecutedTime = exec.ExecutedTime;
        }

        public DbPrivateExecution(BfProductCode productCode, BfChildOrderEvent coe)
        {
            ProductCode = productCode;
            ExecutionId = coe.ExecutionId;
            Side = coe.Side;
            Price = coe.Price;
            Size = coe.Size;
            if (coe.Side == BfTradeSide.Sell)
            {
                Amount = (coe.Price * coe.Size).Truncate(productCode.GetPriceDecimals());
            }
            else
            {
                Amount = (coe.Price * coe.Size).Ceiling(productCode.GetPriceDecimals());
            }

            ChildOrderId = coe.ChildOrderId;
            ChildOrderAcceptanceId = coe.ChildOrderAcceptanceId;

            Commission = coe.Commission;
            ExecutedTime = coe.EventDate;
            SwapForDifference = coe.SwapForDifference;
        }
    }
}
