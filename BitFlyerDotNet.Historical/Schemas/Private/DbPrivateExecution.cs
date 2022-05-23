//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
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
    public class DbPrivateExecution : IBfPrivateExecution
    {
        [Required]
        [Column(Order = 0)]
        public string ProductCode { get; set; }

        [Required]
        [Column(Order = 1)]
        public long ExecutionId { get; set; }

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

        public DbPrivateExecution(string productCode, BfaPrivateExecution exec)
        {
            ProductCode = productCode;
            ExecutionId = exec.ExecutionId;
            Side = exec.Side;
            Price = exec.Price;
            Size = exec.Size;
            if (exec.Side == BfTradeSide.Sell)
            {
                Amount = (exec.Price * exec.Size).Truncate(BfProductCodeEx.GetPriceDecimals(productCode));
            }
            else
            {
                Amount = (exec.Price * exec.Size).Ceiling(BfProductCodeEx.GetPriceDecimals(productCode));
            }

            ChildOrderId = exec.ChildOrderId;
            ChildOrderAcceptanceId = exec.ChildOrderAcceptanceId;

            Commission = exec.Commission;
            ExecutedTime = exec.ExecutedTime;
        }

        public DbPrivateExecution(string productCode, BfChildOrderEvent coe)
        {
            ProductCode = productCode;
            ExecutionId = coe.ExecutionId;
            Side = coe.Side;
            Price = coe.Price;
            Size = coe.Size;
            if (coe.Side == BfTradeSide.Sell)
            {
                Amount = (coe.Price * coe.Size).Truncate(BfProductCodeEx.GetPriceDecimals(productCode));
            }
            else
            {
                Amount = (coe.Price * coe.Size).Ceiling(BfProductCodeEx.GetPriceDecimals(productCode));
            }

            ChildOrderId = coe.ChildOrderId;
            ChildOrderAcceptanceId = coe.ChildOrderAcceptanceId;

            Commission = coe.Commission;
            ExecutedTime = coe.EventDate;
            SwapForDifference = coe.SwapForDifference;
        }
    }
}
