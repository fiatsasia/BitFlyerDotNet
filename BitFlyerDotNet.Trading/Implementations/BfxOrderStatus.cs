//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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

        ConcurrentDictionary<long, BfxExecution> _execs = new();

        internal BfxOrderStatus Update(BfParentOrderStatus status, BfParentOrderDetailStatus detail)
        {
            OrderAcceptanceId = status.ParentOrderAcceptanceId;
            PagingId = status.PagingId;
            OrderId = status.ParentOrderId;
            ProductCode = status.ProductCode;
            OrderType = status.ParentOrderType;
            OrderPrice = status.Price;
            AveragePrice = status.AveragePrice;
            OrderSize = status.Size;
            OrderState = status.ParentOrderState;
            ExpireDate = status.ExpireDate;
            OrderDate = status.ParentOrderDate;
            OutstandingSize = status.OutstandingSize;
            CancelSize = status.CancelSize;
            ExecutedSize = status.ExecutedSize;
            TotalCommission = status.TotalCommission;

            TimeInForce = detail.TimeInForce == BfTimeInForce.NotSpecified ? null : detail.TimeInForce;
            for (int index = 0; index < detail.Parameters.Length; index++)
            {
                Children[index].Update(detail.Parameters[index]);
            }

            throw new NotImplementedException();
        }

        internal BfxOrderStatus Update(BfParentOrder order, string parentOrderAcceptanceId)
        {
            OrderAcceptanceId = parentOrderAcceptanceId;
            ProductCode = order.Parameters[0].ProductCode;
            OrderType = order.OrderMethod;

            for (int index = 0; index < order.Parameters.Count; index++)
            {
                Children[index].Update(order.Parameters[index]);
            }

            throw new NotImplementedException();
        }

        internal BfxOrderStatus UpdateChild(BfParentOrderEvent e)
        {
            OrderType = e.ChildOrderType;
            OrderAcceptanceId = e.ChildOrderAcceptanceId;
            Side = e.Side;
            OrderPrice = e.Price > decimal.Zero ? e.Price : null;
            OrderSize = e.Size;
            ExpireDate = e.ExpireDate;
            return this;
        }

        internal BfxOrderStatus UpdateParent(BfParentOrderEvent e)
        {
            OrderAcceptanceId = e.ParentOrderAcceptanceId;
            OrderId = e.ParentOrderId;
            ProductCode = e.ProductCode;
            OrderType = e.ParentOrderType;
            OrderState = BfOrderState.Active;
            return this;
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

        internal BfxOrderStatus Update(BfChildOrder order, string childOrderAcceptanceId)
        {
            OrderAcceptanceId = childOrderAcceptanceId;

            ProductCode = order.ProductCode;
            OrderType = order.ChildOrderType;
            Side = order.Side;
            OrderPrice = order.Price;
            OrderSize = order.Size;
            MinuteToExpire = order.MinuteToExpire;
            TimeInForce = order.TimeInForce;

            return this;
        }

        public BfxOrderStatus Update(BfChildOrderStatus status, IEnumerable<BfPrivateExecution> execs)
        {
            // Set order
            OrderAcceptanceId = status.ChildOrderAcceptanceId;
            PagingId = status.PagingId;
            OrderId = status.ChildOrderId;
            ProductCode = status.ProductCode;
            Side = status.Side;
            OrderType = status.ChildOrderType;
            OrderPrice = status.Price;
            AveragePrice = status.AveragePrice;
            OrderSize = status.Size;
            OrderState = status.ChildOrderState;
            ExpireDate = status.ExpireDate;
            OrderDate = status.ChildOrderDate;
            OutstandingSize = status.OutstandingSize;
            CancelSize = status.CancelSize;
            ExecutedSize = status.ExecutedSize;
            TotalCommission = status.TotalCommission;

            // Set executions
            foreach (var exec in execs)
            {
                _execs.GetOrAdd(exec.ExecutionId, _ => new BfxExecution(exec));
            }

            return this;
        }

        public void Update(BfChildOrderEvent e)
        {
            OrderAcceptanceId = e.ChildOrderAcceptanceId;
            OrderId = e.ChildOrderId;
            ProductCode = e.ProductCode;
            OrderType = e.ChildOrderType;
            OrderState = BfOrderState.Active;
        }

        public void UpdateExecution(BfChildOrderEvent e)
        {
            _execs.GetOrAdd(e.ExecutionId, _ => new BfxExecution(e));
        }
    }
}
