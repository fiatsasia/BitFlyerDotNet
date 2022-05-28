//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxTransaction : BfxOrderStatus
    {
        public BfxTransactionState State { get; private set; }
        public event EventHandler<BfxTransactionChangedEventArgs> TransactionChanged;

        protected void CancelTransaction() => _cts.Cancel();

        BitFlyerClient _client;
        CancellationTokenSource _cts = new CancellationTokenSource();
        ConcurrentDictionary<long, BfxExecution> _execs = new();

        internal BfxTransaction(BitFlyerClient client)
        {
            _client = client;
        }

        #region Update parent order
        internal BfxTransaction Update(BfParentOrder order, string parentOrderAcceptanceId)
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

        internal BfxTransaction Update(BfParentOrderStatus status, BfParentOrderDetailStatus detail)
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

        internal BfxTransaction OnParentOrderEvent(BfParentOrderEvent e)
        {
            switch (e.EventType)
            {
                case BfOrderEventType.Order:
                    OrderAcceptanceId = e.ParentOrderAcceptanceId;
                    OrderId = e.ParentOrderId;
                    ProductCode = e.ProductCode;
                    OrderType = e.ParentOrderType;
                    OrderState = BfOrderState.Active;
                    break;

                case BfOrderEventType.Trigger:
                    Children[e.ChildOrderIndex].UpdateParentTriggerEvent(e);
                    break;

                case BfOrderEventType.Complete:
                    break;
            }

            throw new NotImplementedException();
        }
        #endregion Update parent order

        #region Update child order
        internal BfxTransaction Update(BfChildOrder order, string childOrderAcceptanceId)
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

        public BfxTransaction Update(BfChildOrderStatus status, IEnumerable<BfPrivateExecution> execs)
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

        internal BfxTransaction OnParentTriggerEvent(BfParentOrderEvent e)
        {
            UpdateParentTriggerEvent(e);
            return this;
        }

        internal BfxTransaction OnChildOrderEvent(BfChildOrderEvent e)
        {
            switch (e.EventType)
            {
                case BfOrderEventType.Order:
                    OrderAcceptanceId = e.ChildOrderAcceptanceId;
                    OrderId = e.ChildOrderId;
                    ProductCode = e.ProductCode;
                    OrderType = e.ChildOrderType;
                    OrderState = BfOrderState.Active;
                    break;

                case BfOrderEventType.Execution:
                    _execs.GetOrAdd(e.ExecutionId, _ => new BfxExecution(e));
                    break;
            }

            throw new NotImplementedException();
        }
        #endregion Update child order

        // - 経過時間でリトライ終了のオプション
        public async Task<string> PlaceOrdertAsync(BfParentOrder order)
        {
            for (var retry = 0; retry <= 3; retry++)
            {
                _cts.Token.ThrowIfCancellationRequested();
                var resp = await _client.SendParentOrderAsync(order, _cts.Token);
                if (!resp.IsError)
                {
                    return resp.GetContent().ParentOrderAcceptanceId;
                }

                _cts.Token.ThrowIfCancellationRequested();
                Log.Info("Trying retry...");
                await Task.Delay(5000);
            }

            Log.Error("SendOrderRequest - Retried out");
            throw new BitFlyerDotNetException();
        }

        public async Task<string> PlaceOrderAsync(BfChildOrder order)
        {
            for (var retry = 0; retry <= 3; retry++)
            {
                _cts.Token.ThrowIfCancellationRequested();
                var resp = await _client.SendChildOrderAsync(order, _cts.Token);
                if (!resp.IsError)
                {
                    return resp.GetContent().ChildOrderAcceptanceId;
                }

                Log.Warn($"SendChildOrder failed: {resp.StatusCode} {resp.ErrorMessage}");
                _cts.Token.ThrowIfCancellationRequested();
                Log.Info("Trying retry...");
                await Task.Delay(3000);
            }

            Log.Error("SendOrderRequest - Retried out");
            throw new BitFlyerDotNetException();
        }

        protected async Task CancelOrderAsync()
        {
            if (State == BfxTransactionState.SendingOrder)
            {
                _cts.Token.ThrowIfCancellationRequested();
            }

            try
            {
                var resp = await _client.CancelParentOrderAsync(ProductCode, OrderId, string.Empty, _cts.Token);
                if (!resp.IsError)
                {
                }
                else
                {
                }
            }
            catch (OperationCanceledException ex)
            {
            }
        }
    }
}
