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
using System.Threading.Tasks;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxMarket : IDisposable
    {
        public bool IsInitialized { get; private set; }

        public event EventHandler<BfxOrderChangedEventArgs>? OrderChanged;

        BitFlyerClient _client;
        BfxPositions _positions = new();
        ConcurrentDictionary<string, BfxTransaction> _orderTransactions = new();

        string _productCode;

        public BfxMarket(BitFlyerClient client, string productCode)
        {
            _client = client;
            _productCode = productCode;
        }

        public void Dispose()
        {
        }

        internal async Task InitializeAsync()
        {
            if (!_client.IsAuthenticated)
            {
                throw new InvalidOperationException($"Client is not authenticated. To Authenticate first.");
            }

            IsInitialized = true;

            // Load active positions from market
            if (_productCode == BfProductCodeEx.FX_BTC_JPY)
            {
                _positions.Update((await _client.GetPositionsAsync(BfProductCodeEx.FX_BTC_JPY)).GetContent());
            }

            // Load active parent orders, their children and executions.
            var updatedChildOrderIds = new HashSet<string>();
            foreach (var parentOrder in (await _client.GetParentOrdersAsync(_productCode, orderState: BfOrderState.Active)).GetContent())
            {
                var parentOrderDetail = (await _client.GetParentOrderDetailAsync(_productCode, parentOrderId: parentOrder.ParentOrderId)).GetContent();
                var txParent = _orderTransactions.AddOrUpdate(parentOrder.ParentOrderAcceptanceId,
                    _ =>
                    {
                        var tx = new BfxTransaction(_client);
                        tx.TransactionChanged += OnTransactionChanged;
                        return tx.Update(parentOrder, parentOrderDetail);
                    },
                    (_, tx) => tx.Update(parentOrder, parentOrderDetail)
                );

                foreach (var childOrder in (await _client.GetChildOrdersAsync(_productCode, parentOrderId: parentOrder.ParentOrderId)).GetContent())
                {
                    updatedChildOrderIds.Add(childOrder.ChildOrderId);
                    var execs = (await _client.GetPrivateExecutionsAsync(_productCode, childOrderId: childOrder.ChildOrderId)).GetContent();
                    var txChild = _orderTransactions.AddOrUpdate(childOrder.ChildOrderAcceptanceId,
                        _ =>
                        {
                            var tx = new BfxTransaction(_client);
                            tx.TransactionChanged += OnTransactionChanged;
                            return tx.Update(childOrder, execs);
                        },
                        (_, tx) => tx.Update(childOrder, execs)
                    );
                }
            }

            // Load active child orders and their executions
            var childOrders = (await _client.GetChildOrdersAsync(_productCode, orderState: BfOrderState.Active)).GetContent();
            foreach (var childOrder in childOrders)
            {
                if (updatedChildOrderIds.Contains(childOrder.ChildOrderId))
                {
                    continue;
                }

                var execs = (await _client.GetPrivateExecutionsAsync(_productCode, childOrderId: childOrder.ChildOrderId)).GetContent();
                _orderTransactions.AddOrUpdate(childOrder.ChildOrderAcceptanceId,
                    _ =>
                    {
                        var tx = new BfxTransaction(_client);
                        tx.TransactionChanged += OnTransactionChanged;
                        return tx.Update(childOrder, execs);
                    },
                    (_, tx) => tx.Update(childOrder, execs)
                );
            }
        }

        internal void OnParentOrderEvent(BfParentOrderEvent e)
        {
            _orderTransactions.AddOrUpdate(e.ParentOrderAcceptanceId,
                _ =>
                {
                    var tx = new BfxTransaction(_client);
                    tx.TransactionChanged += OnTransactionChanged;
                    return tx.OnParentOrderEvent(e);
                },
                (_, tx) => tx.OnParentOrderEvent(e)
            );

            switch (e.EventType)
            {
                case BfOrderEventType.Trigger:
                    _orderTransactions.AddOrUpdate(e.ChildOrderAcceptanceId,
                        _ =>
                        {
                            var tx = new BfxTransaction(_client);
                            tx.TransactionChanged += OnTransactionChanged;
                            return tx.OnParentTriggerEvent(e);
                        },
                        (_, tx) => tx.OnParentTriggerEvent(e)
                    );
                    break;
            }
        }

        internal void OnChildOrderEvent(BfChildOrderEvent e)
        {
            _orderTransactions.AddOrUpdate(e.ChildOrderAcceptanceId,
                _ =>
                {
                    var tx = new BfxTransaction(_client);
                    tx.TransactionChanged += OnTransactionChanged;
                    return tx.OnChildOrderEvent(e);
                },
                (_, tx) => tx.OnChildOrderEvent(e));
        }

        public async Task<BfxTransaction> PlaceOrderAsync(BfParentOrder order)
        {
            // Sometimes parent order event arraives before send order process completes.
            var tx = new BfxTransaction(_client);
            tx.TransactionChanged += OnTransactionChanged;
            var id = await tx.PlaceOrdertAsync(order);
            return _orderTransactions.AddOrUpdate(id, _ => tx, (_, tx) => tx.Update(order, id));
        }

        public async Task<BfxTransaction> PlaceOrderAsync(BfChildOrder order)
        {
            // Sometimes child order event arraives before send order process completes.
            var tx = new BfxTransaction(_client);
            tx.TransactionChanged += OnTransactionChanged;
            var id = await tx.PlaceOrderAsync(order);
            return _orderTransactions.AddOrUpdate(id, _ => tx, (_, tx) => tx.Update(order, id));
        }

        private void OnTransactionChanged(object sender, BfxTransactionChangedEventArgs e)
        {
            switch (e.EvenetType)
            {

            }
        }
    }
}
