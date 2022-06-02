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
    public class BfxTrade
    {
        #region Order informations
        public string ProductCode { get; }
        public BfOrderType OrderType { get; private set; }
        public BfTradeSide? Side { get; private set; }
        public decimal? OrderSize { get; private set; }
        public decimal? OrderPrice { get; private set; }
        public decimal? TriggerPrice { get; private set; }
        public decimal? TrailOffset { get; private set; }
        public int? MinuteToExpire { get; private set; }
        public BfTimeInForce? TimeInForce { get; private set; }
        public ReadOnlyCollection<BfxTrade> Children { get; private set; }
        #endregion Order informations

        #region Order management info
        public string? OrderAcceptanceId { get; internal set; }
        public string? OrderId { get; private set; }
        public DateTime? OrderDate { get; private set; }
        public DateTime? ExpireDate { get; private set; }
        public BfOrderState? OrderState { get; private set; }
        #endregion Order management info

        public uint? PagingId { get; protected set; }
        public decimal? AveragePrice { get; protected set; }
        public decimal? OutstandingSize { get; protected set; }
        public decimal? CancelSize { get; protected set; }
        public decimal? ExecutedPrice { get; protected set; }
        public decimal? ExecutedSize { get; protected set; }
        public decimal? TotalCommission { get; protected set; }

        ConcurrentDictionary<long, BfxExecution> _execs = new();

        internal BfxTrade(string productCode)
        {
            ProductCode = productCode;
        }


        internal void Update(BfParentOrderStatus status, BfParentOrderDetailStatus detail)
        {
            OrderAcceptanceId = status.ParentOrderAcceptanceId;
            PagingId = status.PagingId;
            OrderId = status.ParentOrderId;
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

        internal void Update(BfParentOrder order)
        {
            OrderType = order.OrderMethod;

            for (int index = 0; index < order.Parameters.Count; index++)
            {
                Children[index].Update(order.Parameters[index]);
            }

            throw new NotImplementedException();
        }

        internal void UpdateChild(BfParentOrderEvent e)
        {
            if (e.ChildOrderType.HasValue)
            {
                OrderType = e.ChildOrderType.Value;
            }
            OrderAcceptanceId = e.ChildOrderAcceptanceId;
            Side = e.Side;
            OrderPrice = e.Price > decimal.Zero ? e.Price : null;
            OrderSize = e.Size;
            ExpireDate = e.ExpireDate;
        }

        internal void UpdateChild(int childOrderIndex, BfParentOrderEvent e)
        {
            Children[childOrderIndex].UpdateChild(e);
        }

        internal void UpdateParent(BfParentOrderEvent e)
        {
            OrderAcceptanceId = e.ParentOrderAcceptanceId;
            OrderId = e.ParentOrderId;
            if (e.ParentOrderType.HasValue)
            {
                OrderType = e.ParentOrderType.Value;
            }
            OrderState = BfOrderState.Active;
        }

        internal void Update(BfParentOrderParameter order)
        {
            OrderType = order.ConditionType;
            Side = order.Side;
            OrderPrice = order.Price;
            OrderSize = order.Size;
            TriggerPrice = order.TriggerPrice;
            TrailOffset = order.Offset;
        }

        internal void Update(BfParentOrderDetailStatusParameter order)
        {
            OrderType = order.ConditionType;
            Side = order.Side;
            OrderPrice = order.Price;
            OrderSize = order.Size;
            TriggerPrice = order.TriggerPrice;
            TrailOffset = order.Offset;
        }

        internal void Update(BfChildOrder order, string childOrderAcceptanceId)
        {
            OrderAcceptanceId = childOrderAcceptanceId;

            OrderType = order.ChildOrderType;
            Side = order.Side;
            OrderPrice = order.Price;
            OrderSize = order.Size;
            MinuteToExpire = order.MinuteToExpire;
            TimeInForce = order.TimeInForce;
        }

        public void Update(BfChildOrderStatus status, IEnumerable<BfPrivateExecution> execs)
        {
            // Set order
            OrderAcceptanceId = status.ChildOrderAcceptanceId;
            PagingId = status.PagingId;
            OrderId = status.ChildOrderId;
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
        }

        public void Update(BfChildOrderEvent e)
        {
            OrderAcceptanceId = e.ChildOrderAcceptanceId;
            OrderId = e.ChildOrderId;
            if (e.ChildOrderType.HasValue) OrderType = e.ChildOrderType.Value;
            OrderState = BfOrderState.Active;

            if (e.EventType == BfOrderEventType.Execution)
            {
#pragma warning disable CS8629
                _execs.AddOrUpdate(e.ExecutionId.Value, id => new BfxExecution(e), (id, exec) => exec.Update(e));
#pragma warning restore CS8629
            }
        }

        public void Update(BfPosition[] positions)
        {
            throw new NotImplementedException();
        }
    }
}
