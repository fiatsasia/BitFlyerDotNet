//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Collections.ObjectModel;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxOrderStatus
    {
        public string ProductCode { get; protected set; }
        public BfOrderType OrderType { get; protected set; }
        public BfTradeSide? Side { get; protected set; }
        public decimal? OrderPrice { get; protected set; }
        public decimal? OrderSize { get; protected set; }
        public decimal? TriggerPrice { get; protected set; }
        public decimal? TrailOffset { get; set; }
        public int? MinuteToExpire { get; protected set; }
        public BfTimeInForce? TimeInForce { get; protected set; }

        public uint? PagingId { get; protected set; }
        public decimal? AveragePrice { get; protected set; }
        public string? OrderAcceptanceId { get; protected set; }
        public string? OrderId { get; protected set; }
        public DateTime? OrderDate { get; protected set; }
        public BfOrderState? OrderState { get; protected set; }
        public DateTime? ExpireDate { get; protected set; }
        public decimal? OutstandingSize { get; protected set; }
        public decimal? CancelSize { get; protected set; }
        public decimal? ExecutedPrice { get; protected set; }
        public decimal? ExecutedSize { get; protected set; }
        public decimal? TotalCommission { get; protected set; }

        public ReadOnlyCollection<BfxOrderStatus> Children { get; protected set; }

        internal void UpdateParentTriggerEvent(BfParentOrderEvent e)
        {
            OrderType = e.ChildOrderType;
            OrderAcceptanceId = e.ChildOrderAcceptanceId;
            Side = e.Side;
            OrderPrice = e.Price > decimal.Zero ? e.Price : null;
            OrderSize = e.Size;
            ExpireDate = e.ExpireDate;
        }

        internal void Update(BfParentOrderParameter order)
        {
            ProductCode = order.ProductCode;
            OrderType = order.ConditionType;
            Side = order.Side;
            OrderPrice = order.Price;
            OrderSize = order.Size;
            TriggerPrice = order.TriggerPrice;
            TrailOffset = order.Offset;
        }

        internal void Update(BfParentOrderDetailStatusParameter order)
        {
            ProductCode = order.ProductCode;
            OrderType = order.ConditionType;
            Side = order.Side;
            OrderPrice = order.Price;
            OrderSize = order.Size;
            TriggerPrice = order.TriggerPrice;
            TrailOffset = order.Offset;
        }
    }
}
