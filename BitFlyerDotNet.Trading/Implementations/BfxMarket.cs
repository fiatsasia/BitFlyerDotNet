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
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxMarket : IDisposable
    {
        // Market data sources
        public string ProductCode { get; private set; }
        public BfxConfiguration Config { get; private set; }
        public BitFlyerClient Client => _account.Client;
        public RealtimeSourceFactory RealtimeSource => _account.RealtimeSource;
        public decimal BestAskPrice { get; private set; }
        public decimal BestBidPrice { get; private set; }
        public decimal CurrentPrice => throw new NotImplementedException();
        public decimal LastTradedPrice { get; private set; }

        TimeSpan _serverTimeSpan;
        public DateTime ServerTime => DateTime.UtcNow + _serverTimeSpan;
        public IEnumerable<IBfxTransaction> GetActiveTransactions() =>
            _parentOrderTransactions.Values.Where(e => e.State != BfxTransactionState.Closed).Concat(
                _childOrderTransactions.Values.Where(e => !e.HasParent && e.State != BfxTransactionState.Closed)
            ).ToList(); // ToList() makes thread-safe colleciton

        // Events
        public event EventHandler<BfxOrderChangedEventArgs>? OrderChanged;

        internal IBfOrderSource OrderCache { get; private set; }

        // Private properties
        CompositeDisposable _disposables = new CompositeDisposable();
        BfxAccount _account;
        ConcurrentDictionary<string, IBfxTransaction> _childOrderTransactions = new ConcurrentDictionary<string, IBfxTransaction>();
        ConcurrentDictionary<string, IBfxTransaction> _parentOrderTransactions = new ConcurrentDictionary<string, IBfxTransaction>();
        IObservable<BfTicker> _ticker;

        public BfxMarket(BfxAccount account, string productCode, BfxConfiguration config)
        {
            _account = account;
            ProductCode = productCode;
            Config = config ?? new BfxConfiguration();
            _serverTimeSpan = TimeSpan.Zero;
            _account.RealtimeSource.ConnectionResumed += ConnectionResumed;
        }

        void ConnectionResumed()
        {
            if (OrderCache == default)
            {
                return;
            }

            Task.Run(() =>
            {
                OrderCache.UpdateActiveOrders();
                foreach (var parent in _parentOrderTransactions.Values.ToArray())
                {
                    var xOrder = (BfxParentOrder)parent.Order;
                    xOrder.Update(OrderCache.GetParentOrder(parent.Order.AcceptanceId));
                }

                foreach (var child in _childOrderTransactions.Values.Where(e => !e.HasParent).ToArray())
                {
                    var xOrder = (BfxChildOrder)child.Order;
                    xOrder.Update(OrderCache.GetChildOrder(child.Order.AcceptanceId));
                }
            });
        }

        public BfxMarket(BfxAccount account, string productCode)
            : this(account, productCode, new BfxConfiguration())
        {
            _account = account;
            ProductCode = productCode;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        public void Open(IBfOrderSource orderCache)
        {
            if (orderCache != default)
            {
                OrderCache = orderCache;
            }

            if (_account.Client.IsAuthenticated)
            {
                LoadMarketInformations();
            }

            if (_ticker == null)
            {
                _ticker = _account.RealtimeSource.GetTickerSource(ProductCode);
                _ticker.Subscribe(e =>
                {
                    _serverTimeSpan = e.Timestamp - DateTime.UtcNow;
                    LastTradedPrice = e.LastTradedPrice;
                    BestAskPrice = e.BestAsk;
                    BestBidPrice = e.BestBid;
                }).AddTo(_disposables);
            }
        }

        public IObservable<BfTicker> GetTickerSource()
        {
            return _ticker;
        }

        public void LoadMarketInformations()
        {
            if (OrderCache == default)
            {
                return;
            }

            OrderCache.UpdateActiveOrders();

            // Load active parent orders
            var parents = OrderCache.GetActiveParentOrders();
            foreach (var parent in parents)
            {
                var xParentOrder = new BfxParentOrder(parent);
                var txParent = new BfxParentTransaction(this, xParentOrder);
                foreach (var xChildOrder in xParentOrder.Children.Cast<BfxChildOrder>())
                {
                    _childOrderTransactions[xChildOrder.AcceptanceId] = new BfxChildTransaction(this, xChildOrder, txParent);
                }
                _parentOrderTransactions.TryAdd(parent.AcceptanceId, txParent);
            };

            // Load standalone child orders
            var children = OrderCache.GetActiveIndependentChildOrders();
            foreach (var child in children)
            {
                var order = new BfxChildOrder(child);
                _childOrderTransactions.TryAdd(child.AcceptanceId, new BfxChildTransaction(this, order));
            }
        }

        internal void ForwardChildOrderEvents(BfChildOrderEvent coe)
        {
            OrderCache?.RegisterChildOrderEvent(coe);

            var tx = _childOrderTransactions.GetOrAdd(coe.ChildOrderAcceptanceId, key => new BfxChildTransactionPlaceHolder());
            if (tx is BfxChildTransactionPlaceHolder placeHolder)
            {
                Log.Info($"Child transaction place holder found or placed. {coe.ChildOrderAcceptanceId} {coe.EventType}");
                placeHolder.ChildOrderEvents.Add(coe);
                return;
            }

            if (!(tx is BfxChildTransaction txChild))
            {
                throw new ApplicationException();
            }

            if (txChild.Parent != null)
            {
                txChild.Parent.OnChildOrderEvent(coe);
            }
            else
            {
                txChild.OnChildOrderEvent(coe);
            }
        }

        internal void ForwardParentOrderEvents(BfParentOrderEvent poe)
        {
            OrderCache?.RegisterParentOrderEvent(poe);

            // Sometimes parent order event arraives faster than send order process completes.
            var tx = _parentOrderTransactions.GetOrAdd(poe.ParentOrderAcceptanceId, key => new BfxParentTransactionPlaceHolder());
            if (tx is BfxParentTransactionPlaceHolder placeHolder)
            {
                Log.Info($"Parent transaction place holder found or placed. {poe.ParentOrderAcceptanceId} {poe.EventType}");
                placeHolder.ParentOrderEvents.Add(poe);
                return;
            }

            if (!(tx is BfxParentTransaction txParent))
            {
                throw new ApplicationException();
            }

            txParent.OnParentOrderEvent(poe);

            if (poe.EventType == BfOrderEventType.Trigger)
            {
                if (!(txParent.Order.Children[poe.ChildOrderIndex - 1] is BfxChildOrder childOrder))
                {
                    throw new ApplicationException();
                }

                if (!string.IsNullOrEmpty(childOrder.AcceptanceId))
                {
                    var txChild = new BfxChildTransaction(this, childOrder, txParent);
                    _childOrderTransactions.AddOrUpdate(childOrder.AcceptanceId, txChild, (key, value) =>
                    {
                        Log.Info($"Child transaction place holder found and merged to parent. {childOrder.AcceptanceId} Parent.{poe.EventType}");
                        if (!(value is BfxChildTransactionPlaceHolder placeHolder))
                        {
                            throw new ApplicationException();
                        }

                        // ToList() prevents "Collection was modified; enumeration operation may not execute."
                        placeHolder.ChildOrderEvents.ToList().ForEach(coe => txParent.OnChildOrderEvent(coe));
                        return txChild;
                    });
                }
            }
        }

        public IBfxTransaction CreateTransaction(IBfxOrder order, BfTimeInForce timeInForce, TimeSpan periodToExpire)
        {
            switch (order)
            {
                case BfxChildOrder child:
                    child.ApplyParameters(ProductCode, Convert.ToInt32(periodToExpire.TotalMinutes), timeInForce);
                    return new BfxChildTransaction(this, child);

                case BfxParentOrder parent:
                    parent.ApplyParameters(ProductCode, Convert.ToInt32(periodToExpire.TotalMinutes), timeInForce);
                    return new BfxParentTransaction(this, parent);

                default:
                    throw new NotSupportedException();
            }
        }

        public IBfxTransaction CreateTransaction(IBfxOrder order)
        {
            return CreateTransaction(order, BfTimeInForce.NotSpecified, TimeSpan.Zero);
        }

        public Task DispatchTransaction(IBfxTransaction tx)
        {
            switch (tx)
            {
                case BfxChildTransaction child:
                    return child.SendOrderRequestAsync();

                case BfxParentTransaction parent:
                    return parent.SendOrderRequestAsync();

                default:
                    throw new NotSupportedException();
            }
        }

        public IBfxTransaction PlaceOrder(IBfxOrder order, TimeSpan periodToExpire, BfTimeInForce timeInForce)
        {
            var tx = CreateTransaction(order, timeInForce, periodToExpire);
            _ = DispatchTransaction(tx);
            return tx;
        }

        public IBfxTransaction PlaceOrder(IBfxOrder order)
        {
            return PlaceOrder(order, TimeSpan.Zero, BfTimeInForce.NotSpecified);
        }

        internal void RegisterTransaction(BfxChildTransaction tx)
        {
            _childOrderTransactions.AddOrUpdate(tx.MarketId, tx, (key, value) =>
            {
                Log.Info($"Child transaction place holder found after order sent. {tx.MarketId}");
                if (!(value is BfxChildTransactionPlaceHolder placeHolder))
                {
                    throw new ApplicationException();
                }

                // ToList() prevents "Collection was modified; enumeration operation may not execute."
                placeHolder.ChildOrderEvents.ToList().ForEach(coe => tx.OnChildOrderEvent(coe));
                return tx;
            });
        }

        internal void RegisterTransaction(BfxParentTransaction tx)
        {
            _parentOrderTransactions.AddOrUpdate(tx.MarketId, tx, (key, value) =>
            {
                Log.Info($"Parent transaction place holder found after order sent. {tx.MarketId}");
                if (!(value is BfxParentTransactionPlaceHolder placeHolder))
                {
                    throw new ApplicationException();
                }
                foreach (var poe in placeHolder.ParentOrderEvents)
                {
                    tx.OnParentOrderEvent(poe);
                    if (poe.EventType == BfOrderEventType.Trigger)
                    {
                        RegisterTransaction(new BfxChildTransaction(this, (BfxChildOrder)tx.Order.Children[poe.ChildOrderIndex - 1], tx));
                    }
                }
                return tx;
            });
        }

        internal void InvokeOrderTransactionEvent(object sender, BfxOrderChangedEventArgs ev)
        {
            switch (ev.EventType)
            {
                case BfxOrderEventType.OrderSent:
                    break;

                    // トランザクション削除
            }

            OrderChanged?.Invoke(sender, ev);
        }
    }
}
