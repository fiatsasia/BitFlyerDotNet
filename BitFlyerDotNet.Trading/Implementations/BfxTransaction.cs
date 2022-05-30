//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxTransaction : BfxOrderStatus
    {
        public BfxTransactionState State { get; private set; }
        public event EventHandler<BfxTransactionChangedEventArgs>? TransactionChanged;

        BitFlyerClient _client;
        CancellationTokenSource _cts = new CancellationTokenSource();

        internal BfxTransaction(BitFlyerClient client)
        {
            _client = client;
        }

        #region Child order
        internal new BfxTransaction Update(BfChildOrder order, string childOrderAcceptanceId) => Update(order, childOrderAcceptanceId);
        public new BfxTransaction Update(BfChildOrderStatus status, IEnumerable<BfPrivateExecution> execs) => (BfxTransaction)base.Update(status, execs);

        internal BfxTransaction OnParentOrderEventForChildren(BfParentOrderEvent e)
        {
            var result = (BfxTransaction)base.UpdateChild(e);
            switch (e.EventType)
            {
                case BfOrderEventType.Trigger:
                    break;

                case BfOrderEventType.Complete:
                    break;
            }
            return result;
        }

        internal BfxTransaction OnChildOrderEvent(BfChildOrderEvent e)
        {
            switch (e.EventType)
            {
                case BfOrderEventType.Order:
                    Update(e);
                    break;

                case BfOrderEventType.Execution:
                    UpdateExecution(e);
                    break;
            }
            return this;
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
        #endregion Child order

        #region Parent order
        internal new BfxTransaction Update(BfParentOrder order, string parentOrderAcceptanceId) => (BfxTransaction)base.Update(order, parentOrderAcceptanceId);
        internal new BfxTransaction Update(BfParentOrderStatus status, BfParentOrderDetailStatus detail) => (BfxTransaction)base.Update(status, detail);

        internal BfxTransaction OnParentOrderEvent(BfParentOrderEvent e)
        {
            switch (e.EventType)
            {
                case BfOrderEventType.Order:
                    UpdateParent(e);
                    break;

                case BfOrderEventType.Trigger:
                    Children[e.ChildOrderIndex].UpdateChild(e);
                    break;

                case BfOrderEventType.Complete:
                    break;
            }
            return this;
        }

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
        #endregion Parent order

        protected async Task CancelOrderAsync()
        {
            //protected void CancelTransaction() => _cts.Cancel();

            if (State == BfxTransactionState.SendingOrder)
            {
                _cts.Token.ThrowIfCancellationRequested();
            }

            try
            {
                var resp = await _client.CancelParentOrderAsync(ProductCode, string.Empty, OrderAcceptanceId, _cts.Token);
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
