//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using BitFlyerDotNet.LightningApi;
using System.Threading.Tasks;

namespace BitFlyerDotNet.Trading
{
    public class BfxMarket : IDisposable
    {
        // Market data sources
        public BfProductCode ProductCode { get; private set; }
        public BfxConfiguration Config { get; private set; }
        public BitFlyerClient Client => _account.Client;
        public RealtimeSourceFactory RealtimeSource => _account.RealtimeSource;
        public decimal BestAskPrice { get; private set; }
        public decimal BestBidPrice { get; private set; }
        public decimal LastTradedPrice { get; private set; }

        TimeSpan _serverTimeSpan;
        public DateTime ServerTime => DateTime.UtcNow + _serverTimeSpan;
        public IEnumerable<IBfxOrderTransaction> GetActiveTransactions() =>
            _parentOrderTransactions.Values.Where(e => e.State != BfxOrderTransactionState.Closed).Concat(
                _childOrderTransactions.Values.Where(e => !e.HasParent && e.State != BfxOrderTransactionState.Closed)
            ).ToList(); // ToList() makes thread-safe colleciton

        // Events
        public event EventHandler<BfxOrderTransactionEventArgs>? OrderTransactionChanged;

        // Private properties
        CompositeDisposable _disposables = new CompositeDisposable();
        BfxAccount _account;
        ConcurrentDictionary<string, IBfxOrderTransaction> _childOrderTransactions = new ConcurrentDictionary<string, IBfxOrderTransaction>();
        ConcurrentDictionary<string, IBfxOrderTransaction> _parentOrderTransactions = new ConcurrentDictionary<string, IBfxOrderTransaction>();
        IObservable<BfxTicker> _ticker;

        public BfxMarket(BfxAccount account, BfProductCode productCode, BfxConfiguration config)
        {
            _account = account;
            ProductCode = productCode;
            Config = config ?? new BfxConfiguration();

            _serverTimeSpan = TimeSpan.Zero;
            _ticker = new BfxTickerSource(this).Publish().RefCount();
        }

        public BfxMarket(BfxAccount account, BfProductCode productCode)
            : this(account, productCode, new BfxConfiguration())
        {
            _account = account;
            ProductCode = productCode;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        public void Open()
        {
            LoadMarketInformations();
            _ticker.Subscribe(e =>
            {
                _serverTimeSpan = e.ServerTimeDiff;
                LastTradedPrice = e.LastTradedPrice;
                BestAskPrice = e.BestAskPrice;
                BestBidPrice = e.BestBidPrice;
            }).AddTo(_disposables);
        }

        public IObservable<BfxTicker> GetTickerSource()
        {
            return _ticker;
        }

        public void LoadMarketInformations()
        {
            var childOrders = _account.Client.GetChildOrders(ProductCode, BfOrderState.Active).GetContent().ToDictionary(e => e.ChildOrderAcceptanceId);

            // Load active parent orders
            foreach (var parentOrder in _account.Client.GetParentOrders(ProductCode, orderState: BfOrderState.Active).GetContent())
            {
                var xParentOrder = new BfxParentOrder(Client, ProductCode, parentOrder);
                var txParent = new BfxParentOrderTransaction(this, xParentOrder);
                foreach (var xChildOrder in xParentOrder.Children.Cast<BfxChildOrder>())
                {
                    _childOrderTransactions[xChildOrder.ChildOrderAcceptanceId] = new BfxChildOrderTransaction(this, xChildOrder, txParent);
                    childOrders.Remove(xChildOrder.ChildOrderAcceptanceId); // remove child of parent
                }
                _parentOrderTransactions.TryAdd(parentOrder.ParentOrderAcceptanceId, txParent);
            };

            // Load standalone child orders
            foreach (var child in childOrders.Values)
            {
                var execs = Client.GetPrivateExecutions(ProductCode, childOrderId: child.ChildOrderId).GetContent();
                var order = new BfxChildOrder(ProductCode, child);
                order.Update(execs);
                _childOrderTransactions.TryAdd(child.ChildOrderAcceptanceId, new BfxChildOrderTransaction(this, order));
            }
        }

        internal void TryOpen()
        {
            if (Config == null)
            {
                Open();
            }
        }

        internal void ForwardChildOrderEvents(BfChildOrderEvent coe)
        {
            var tx = _childOrderTransactions.GetOrAdd(coe.ChildOrderAcceptanceId, key => new BfxChildTransactionPlaceHolder());
            if (tx is BfxChildTransactionPlaceHolder placeHolder)
            {
                Debug.WriteLine($"--Child transaction place holder found or placed. {coe.ChildOrderAcceptanceId} {coe.EventType}");
                placeHolder.ChildOrderEvents.Add(coe);
                return;
            }

            if (!(tx is BfxChildOrderTransaction txChild))
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
            // Sometimes parent order event arraives faster than send order process completes.
            var tx = _parentOrderTransactions.GetOrAdd(poe.ParentOrderAcceptanceId, key => new BfxParentTransactionPlaceHolder());
            if (tx is BfxParentTransactionPlaceHolder placeHolder)
            {
                Debug.WriteLine($"--Parent transaction place holder found or placed. {poe.ParentOrderAcceptanceId} {poe.EventType}");
                placeHolder.ParentOrderEvents.Add(poe);
                return;
            }

            if (!(tx is BfxParentOrderTransaction txParent))
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

                if (!string.IsNullOrEmpty(childOrder.ChildOrderAcceptanceId))
                {
                    var txChild = new BfxChildOrderTransaction(this, childOrder, txParent);
                    _childOrderTransactions.AddOrUpdate(childOrder.ChildOrderAcceptanceId, txChild, (key, value) =>
                    {
                        Debug.WriteLine($"--Child transaction place holder found and merged to parent. {childOrder.ChildOrderAcceptanceId} Parent.{poe.EventType}");
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

        public IBfxOrderTransaction CreateTransaction(IBfxOrder order, TimeSpan periodToExpire, BfTimeInForce timeInForce)
        {
            switch (order)
            {
                case BfxChildOrder child:
                    child.ApplyParameters(ProductCode, Convert.ToInt32(periodToExpire.TotalMinutes), timeInForce);
                    return new BfxChildOrderTransaction(this, child);

                case BfxParentOrder parent:
                    parent.ApplyParameters(ProductCode, Convert.ToInt32(periodToExpire.TotalMinutes), timeInForce);
                    return new BfxParentOrderTransaction(this, parent);

                default:
                    throw new NotSupportedException();
            }
        }

        public IBfxOrderTransaction CreateTransaction(IBfxOrder order)
        {
            return CreateTransaction(order, TimeSpan.Zero, BfTimeInForce.NotSpecified);
        }

        public Task DispatchTransaction(IBfxOrderTransaction tx)
        {
            switch (tx)
            {
                case BfxChildOrderTransaction child:
                    return child.SendOrderRequestAsync();

                case BfxParentOrderTransaction parent:
                    return parent.SendOrderRequestAsync();

                default:
                    throw new NotSupportedException();
            }
        }

        public IBfxOrderTransaction PlaceOrder(IBfxOrder order, TimeSpan periodToExpire, BfTimeInForce timeInForce)
        {
            TryOpen();
            var tx = CreateTransaction(order, periodToExpire, timeInForce);
            _ = DispatchTransaction(tx);
            return tx;
        }

        public IBfxOrderTransaction PlaceOrder(IBfxOrder order)
        {
            return PlaceOrder(order, TimeSpan.Zero, BfTimeInForce.NotSpecified);
        }

        internal void RegisterTransaction(BfxChildOrderTransaction tx)
        {
            _childOrderTransactions.AddOrUpdate(tx.MarketId, tx, (key, value) =>
            {
                Debug.WriteLine($"--Child transaction place holder found after order sent. {tx.MarketId}");
                if (!(value is BfxChildTransactionPlaceHolder placeHolder))
                {
                    throw new ApplicationException();
                }

                // ToList() prevents "Collection was modified; enumeration operation may not execute."
                placeHolder.ChildOrderEvents.ToList().ForEach(coe => tx.OnChildOrderEvent(coe));
                return tx;
            });
        }

        internal void RegisterTransaction(BfxParentOrderTransaction tx)
        {
            _parentOrderTransactions.AddOrUpdate(tx.MarketId, tx, (key, value) =>
            {
                Debug.WriteLine($"--Parent transaction place holder found after order sent. {tx.MarketId}");
                if (!(value is BfxParentTransactionPlaceHolder placeHolder))
                {
                    throw new ApplicationException();
                }
                foreach (var poe in placeHolder.ParentOrderEvents)
                {
                    tx.OnParentOrderEvent(poe);
                    if (poe.EventType == BfOrderEventType.Trigger)
                    {
                        RegisterTransaction(new BfxChildOrderTransaction(this, (BfxChildOrder)tx.Order.Children[poe.ChildOrderIndex - 1], tx));
                    }
                }
                return tx;
            });
        }

        internal void InvokeOrderTransactionEvent(object sender, BfxOrderTransactionEventArgs ev)
        {
            switch (ev.EventType)
            {
                case BfxOrderTransactionEventType.OrderSent:
                    break;

                    // トランザクション削除
            }

            OrderTransactionChanged?.Invoke(sender, ev);
        }
    }
}
