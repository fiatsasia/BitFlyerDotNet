//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Historical;

class DbChildOrderStatusMetadata
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("child_order_id")]
    public string ChildOrderId { get; set; }

    [Column("product_code")]
    public string ProductCode { get; set; }

    [Column("side")]
    public BfTradeSide Side { get; set; }

    [Column("child_order_type")]
    public BfOrderType ChildOrderType { get; set; }

    [Column("price")]
    public decimal? Price { get; set; }

    [Column("average_price")]
    public decimal AveragePrice { get; set; }

    [Column("size")]
    public decimal Size { get; set; }

    [Column("child_order_state")]
    public BfOrderState ChildOrderState { get; set; }

    [Column("expire_date")]
    public DateTime ExpireDate { get; set; }

    [Column("child_order_date")]
    public DateTime ChildOrderDate { get; set; }

    [Column("child_order_acceptance_id")]
    public string ChildOrderAcceptanceId { get; set; }

    [Column("outstanding_size")]
    public decimal OutstandingSize { get; set; }

    [Column("cancel_size")]
    public decimal CancelSize { get; set; }

    [Column("executed_size")]
    public decimal ExecutedSize { get; set; }

    [Column("total_commission")]
    public decimal TotalCommission { get; set; }
}

[MetadataType(typeof(DbChildOrderStatusMetadata))]
class DbChildOrderStatus : BfChildOrderStatus
{
}
