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
    public class BfxTransaction
    {
        public BfxTransactionState State { get; private set; }
        public event EventHandler<BfxTransactionChangedEventArgs>? TransactionChanged;

        BitFlyerClient _client;
        CancellationTokenSource _cts = new CancellationTokenSource();
        BfxTrade _trade;
        BfxConfiguration _config;

        internal BfxTransaction(BitFlyerClient client, string productCode, BfxConfiguration config)
        {
            _client = client;
            _config = config;
            _trade = new BfxTrade(productCode);
        }

        #region Child order
        internal BfxTransaction Update(BfChildOrder order)
        {
            _trade.Update(order);
            return this;
        }
        public BfxTransaction Update(BfChildOrderStatus status, IEnumerable<BfPrivateExecution> execs)
        {
            _trade.Update(status, execs);
            return this;
        }

        internal BfxTransaction OnParentOrderEventForChildren(BfParentOrderEvent e)
        {
            _trade.UpdateChild(e);
            switch (e.EventType)
            {
                case BfOrderEventType.Trigger:
                    break;

                case BfOrderEventType.Complete:
                    break;
            }
            return this;
        }

        internal BfxTransaction OnChildOrderEvent(BfChildOrderEvent e)
        {
            switch (e.EventType)
            {
                case BfOrderEventType.Order:
                case BfOrderEventType.Execution:
                    _trade.Update(e);
                    break;
            }
            return this;
        }

        public async Task<string> PlaceOrderAsync(BfChildOrder order)
        {
            _trade.Update(order);
            for (var retry = 0; retry <= _config.OrderRetryMax; retry++)
            {
                _cts.Token.ThrowIfCancellationRequested();
                var resp = await _client.SendChildOrderAsync(order, _cts.Token);
                if (!resp.IsError)
                {
                    var id = resp.GetContent().ChildOrderAcceptanceId;
                    _trade.OrderAcceptanceId = id;
                    return id;
                }

                Log.Warn($"SendChildOrder failed: {resp.StatusCode} {resp.ErrorMessage}");
                _cts.Token.ThrowIfCancellationRequested();
                Log.Info("Trying retry...");
                await Task.Delay(_config.OrderRetryInterval);
            }

            Log.Error("SendOrderRequest - Retried out");
            throw new BitFlyerDotNetException();
        }
        #endregion Child order

        #region Parent order
        internal BfxTransaction Update(BfParentOrder order)
        {
            _trade.Update(order);
            return this;
        }
        internal BfxTransaction Update(BfParentOrderStatus status, BfParentOrderDetailStatus detail)
        {
            _trade.Update(status, detail);
            return this;
        }

#pragma warning disable CS8629
        internal BfxTransaction OnParentOrderEvent(BfParentOrderEvent e)
        {
            switch (e.EventType)
            {
                case BfOrderEventType.Order:
                    _trade.UpdateParent(e);
                    break;

                case BfOrderEventType.Trigger:
                    _trade.UpdateChild(e.ChildOrderIndex.Value - 1, e);
                    break;

                case BfOrderEventType.Complete:
                    break;
            }
            return this;
        }
#pragma warning restore CS8629

        // - 経過時間でリトライ終了のオプション
        public async Task<string> PlaceOrdertAsync(BfParentOrder order)
        {
            _trade.Update(order);
            for (var retry = 0; retry <= _config.OrderRetryMax; retry++)
            {
                _cts.Token.ThrowIfCancellationRequested();
                var resp = await _client.SendParentOrderAsync(order, _cts.Token);
                if (!resp.IsError)
                {
                    var id = resp.GetContent().ParentOrderAcceptanceId;
                    _trade.OrderAcceptanceId = id;
                    return id;
                }

                _cts.Token.ThrowIfCancellationRequested();
                Log.Info("Trying retry...");
                await Task.Delay(_config.OrderRetryInterval);
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
                var resp = await _client.CancelParentOrderAsync(_trade.ProductCode, string.Empty, _trade.OrderAcceptanceId, _cts.Token);
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
