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
    public class DbBalance
    {
        [Key]
        [Column(Order = 0)]
        public int Id { get; set; }

        [Required]
        [Column(Order = 1)]
        public DateTime Date { get; set; }

        [Required]
        [Column(Order = 2)]
        public string ProductCode { get; set; }

        [Required]
        [Column(Order = 3)]
        public BfCurrencyCode CurrencyCode { get; set; }

        [Required]
        [Column(Order = 4)]
        public BfTradeType TradeType { get; set; }

        [Required]
        [Column(Order = 5)]
        public decimal Price { get; set; }

        [Required]
        [Column(Order = 6)]
        public decimal Amount { get; set; }

        [Required]
        [Column(Order = 7)]
        public decimal Quantity { get; set; }

        [Required]
        [Column(Order = 8)]
        public decimal Commission { get; set; }

        [Required]
        [Column(Order = 9)]
        public decimal Balance { get; set; }

        [Required]
        [Column(Order = 10)]
        public string OrderId { get; set; }

        public DbBalance()
        {
        }

        public DbBalance(BfBalanceHistory balance)
        {
            Id = balance.PagingId;
            Date = balance.EventDate;
            ProductCode = balance.ProductCode;
            CurrencyCode = balance.CurrencyCode;
            TradeType = balance.TradeType;
            Price = balance.Price;
            Amount = balance.Amount;
            Quantity = balance.Quantity;
            Commission = balance.Commission;
            Balance = balance.Balance;
            OrderId = balance.OrderId;
        }
    }
}
