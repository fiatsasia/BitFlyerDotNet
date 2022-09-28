//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.DataSource;

class DbChildOrderStatus : BfChildOrderStatus
{
    [Column(Name = "id", PrimaryKey = true)]
    public override long Id { get; set; }

    [Column("child_order_id", Index = true)]
    public override string ChildOrderId { get; set; }

    [Column("product_code", Index = true, IndexOrder = 0, SortOrder = SortOrder.Ascending)]
    public override string ProductCode { get; set; }

    [Column("side", EnumMember = true)]
    public override BfTradeSide Side { get; set; }

    [Column("child_order_type", EnumMember = true, Index = true)]
    public override BfOrderType ChildOrderType { get; set; }

    [Column("price")]
    public override decimal Price { get; set; }

    [Column("average_price")]
    public override decimal AveragePrice { get; set; }

    [Column("size")]
    public override decimal Size { get; set; }

    [Column("child_order_state", EnumMember = true, Index = true, IndexOrder = 1, SortOrder = SortOrder.Ascending)]
    public override BfOrderState ChildOrderState { get; set; }

    [Column("expire_date", Index = true, IndexOrder = 4, SortOrder = SortOrder.Descending)]
    public override DateTime ExpireDate { get; set; }

    [Column("child_order_date", Index = true, IndexOrder = 3, SortOrder = SortOrder.Descending)]
    public override DateTime ChildOrderDate { get; set; }

    [Column("child_order_acceptance_id", Index = true, IndexOrder = 2)]
    public override string ChildOrderAcceptanceId { get; set; }

    [Column("outstanding_size")]
    public override decimal OutstandingSize { get; set; }

    [Column("cancel_size")]
    public override decimal CancelSize { get; set; }

    [Column("executed_size")]
    public override decimal ExecutedSize { get; set; }

    [Column("total_commission")]
    public override decimal TotalCommission { get; set; }
}
