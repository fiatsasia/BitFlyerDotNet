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
    public class DbChildOrder
    {
        [Key]
        [Column(Order = 0)]
        public uint PagingId { get; private set; }

        [Required]
        [Column(Order = 1)]
        public string ChildOrderId { get; private set; }

        [Required]
        [Column(Order = 2)]
        public string ProductCode { get; private set; }

        [Required]
        [Column(Order = 3)]
        public BfTradeSide Side { get; private set; }

        [Required]
        [Column(Order = 4)]
        public BfOrderType ChildOrderType { get; private set; }

        [Required]
        [Column(Order = 5)]
        public decimal Price { get; private set; } // value is 0 when executed by market price

        [Required]
        [Column(Order = 6)]
        public decimal AveragePrice { get; private set; }

        [Required]
        [Column(Order = 7)]
        public decimal Size { get; private set; }

        [Required]
        [Column(Order = 8)]
        public BfOrderState ChildOrderState { get; private set; }

        [Required]
        [Column(Order = 9)]
        public DateTime ExpireDate { get; private set; }

        [Required]
        [Column(Order = 10)]
        public DateTime ChildOrderDate { get; private set; }

        [Required]
        [Column(Order = 11)]
        public string ChildOrderAcceptanceId { get; private set; }

        [Required]
        [Column(Order = 12)]
        public decimal OutstandingSize { get; private set; }

        [Required]
        [Column(Order = 13)]
        public decimal CancelSize { get; private set; }

        [Required]
        [Column(Order = 14)]
        public decimal ExecutedSize { get; private set; }

        [Required]
        [Column(Order = 15)]
        public decimal TotalCommission { get; private set; }

        public DbChildOrder()
        {
        }

        public DbChildOrder(BfChildOrder order)
        {
            PagingId = order.PagingId;
            ChildOrderId = order.ChildOrderId;
            ProductCode = order.ProductCode;
            Side = order.Side;
            ChildOrderType = order.ChildOrderType;
            Price = order.Price;
            AveragePrice = order.AveragePrice;
            Size = order.Size;
            ChildOrderState = order.ChildOrderState;
            ExpireDate = order.ExpireDate;
            ChildOrderDate = order.ChildOrderDate;
            ChildOrderAcceptanceId = order.ChildOrderAcceptanceId;
            OutstandingSize = order.OutstandingSize;
            CancelSize = order.CancelSize;
            ExecutedSize = order.ExecutedSize;
            TotalCommission = order.TotalCommission;
        }
    }
}
