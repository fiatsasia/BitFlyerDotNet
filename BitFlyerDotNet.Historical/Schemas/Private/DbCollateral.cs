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
    public class DbCollateral
    {
        [Key]
        [Column(Order = 0)]
        public long Id { get; set; }

        [Required]
        [Column(Order = 1)]
        public string CurrencyCode { get; set; }

        [Required]
        [Column(Order = 2)]
        public decimal Change { get; set; }

        [Required]
        [Column(Order = 3)]
        public decimal Amount { get; set; }

        [Required]
        [Column(Order = 4)]
        public string ReasonCode { get; set; }

        [Required]
        [Column(Order = 5)]
        public DateTime Date { get; set; }

        public DbCollateral()
        {
        }

        public DbCollateral(BfCollateralHistory coll)
        {
            Id = coll.Id;
            CurrencyCode = coll.CurrencyCode;
            Change = coll.Change;
            Amount = coll.Amount;
            ReasonCode = coll.ReasonCode;
            Date = coll.Date;
        }
    }
}
