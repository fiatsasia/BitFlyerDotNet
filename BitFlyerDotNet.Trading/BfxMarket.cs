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

        public BfxMarket(BfxAccount account, BfProductCode productCode, BfxConfiguration config)
        {
            _account = account;
            ProductCode = productCode;
            Config = config ?? new BfxConfiguration();

            _serverTimeSpan = TimeSpan.Zero;
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
            RealtimeSource.GetExecutionSource(ProductCode).Subscribe(e => { _serverTimeSpan = e.ExecutedTime - DateTime.UtcNow; LastTradedPrice = e.Price; }).AddTo(_disposables);
            RealtimeSource.GetOrderBookSource(ProductCode).Subscribe(e => { BestAskPrice = e.BestAskPrice; BestBidPrice = e.BestBidPrice; }).AddTo(_disposables);
        }

        public void LoadMarketInformations()
        {
            var childOrders = _account.Client.GetChildOrders(ProductCode, BfOrderState.Active).GetContent().ToDictionary(e => e.ChildOrderAcceptanceId);

            // Load active parent orders
            foreach (var parentOrder in _account.Client.GetParentOrders(ProductCode, orderState: BfOrderState.Active).GetContent())
            {
                var xParentOrder = new BfxParentOrder(Client, ProductCode, parentOrder);
                var parentTran = new BfxParentOrderTransaction(this, xParentOrder, OnOrderTransactionEvent);
                foreach (var xChildOrder in xParentOrder.Children.Cast<BfxChildOrder>())
                {
                    var childTran = new BfxChildOrderTransaction(this, xChildOrder, parentTran, OnOrderTransactionEvent);
                    _childOrderTransactions[xChildOrder.ChildOrderAcceptanceId] = childTran;
                    childOrders.Remove(xChildOrder.ChildOrderAcceptanceId); // remove child of parent
                }
                _parentOrderTransactions.TryAdd(parentOrder.ParentOrderAcceptanceId, parentTran);
            };

            // Load standalone child orders
            foreach (var child in childOrders.Values)
            {
                var execs = Client.GetPrivateExecutions(ProductCode, childOrderId: child.ChildOrderId).GetContent();
                var order = new BfxChildOrder(ProductCode, child);
                order.Update(execs);
                _childOrderTransactions.TryAdd(child.ChildOrderAcceptanceId, new BfxChildOrderTransaction(this, order, OnOrderTransactionEvent));
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
            var tran = _childOrderTransactions.GetOrAdd(coe.ChildOrderAcceptanceId, key => new BfxChildTransactionPlaceHolder());
            if (tran is BfxChildTransactionPlaceHolder placeHolder)
            {
                Debug.WriteLine($"--Child transaction place holder found or placed. {coe.ChildOrderAcceptanceId} {coe.EventType}");
                placeHolder.ChildOrderEvents.Add(coe);
                return;
            }

            if (!(tran is BfxChildOrderTransaction childTran))
            {
                throw new ApplicationException();
            }

            if (childTran.Parent != null)
            {
                childTran.Parent.OnChildOrderEvent(coe);
            }
            else
            {
                childTran.OnChildOrderEvent(coe);
            }
        }

        internal void ForwardParentOrderEvents(BfParentOrderEvent poe)
        {
            // Sometimes parent order event arraives faster than send order process completes.
            var tran = _parentOrderTransactions.GetOrAdd(poe.ParentOrderAcceptanceId, key => new BfxParentTransactionPlaceHolder());
            if (tran is BfxParentTransactionPlaceHolder placeHolder)
            {
                Debug.WriteLine($"--Parent transaction place holder found or placed. {poe.ParentOrderAcceptanceId} {poe.EventType}");
                placeHolder.ParentOrderEvents.Add(poe);
                return;
            }

            if (!(tran is BfxParentOrderTransaction parentTran))
            {
                throw new ApplicationException();
            }

            parentTran.OnParentOrderEvent(poe);

            if (poe.EventType == BfOrderEventType.Trigger)
            {
                if (!(parentTran.Order.Children[poe.ChildOrderIndex - 1] is BfxChildOrder childOrder))
                {
                    throw new ApplicationException();
                }

                if (!string.IsNullOrEmpty(childOrder.ChildOrderAcceptanceId))
                {
                    var childTran = new BfxChildOrderTransaction(this, childOrder, parentTran, OnOrderTransactionEvent);
                    _childOrderTransactions.AddOrUpdate(childOrder.ChildOrderAcceptanceId, childTran, (key, value) =>
                    {
                        Debug.WriteLine($"--Child transaction place holder found and merged to parent. {childOrder.ChildOrderAcceptanceId} Parent.{poe.EventType}");
                        if (!(value is BfxChildTransactionPlaceHolder placeHolder))
                        {
                            throw new ApplicationException();
                        }

                        // ToList() prevents "Collection was modified; enumeration operation may not execute."
                        placeHolder.ChildOrderEvents.ToList().ForEach(coe => parentTran.OnChildOrderEvent(coe));
                        return childTran;
                    });
                }
            }
        }

        public IBfxOrderTransaction PlaceOrder(IBfxOrder order, TimeSpan periodToExpire, BfTimeInForce timeInForce)
        {
            TryOpen();
            switch (order)
            {
                case BfxChildOrder child:
                    return SendChildOrder(child, periodToExpire, timeInForce);

                case BfxParentOrder parent:
                    return SendParentOrder(parent, periodToExpire, timeInForce);

                default:
                    throw new NotSupportedException();
            }
        }

        public IBfxOrderTransaction PlaceOrder(IBfxOrder order)
        {
            return PlaceOrder(order, TimeSpan.Zero, BfTimeInForce.NotSpecified);
        }

        BfxChildOrderTransaction SendChildOrder(BfxChildOrder order, TimeSpan periodToExpire, BfTimeInForce timeInForce)
        {
            order.ApplyParameters(ProductCode, Convert.ToInt32(periodToExpire.TotalMinutes), timeInForce);
            var trans = new BfxChildOrderTransaction(this, order, OnOrderTransactionEvent);
            _ = trans.SendOrderRequestAsync();
            return trans;
        }

        BfxParentOrderTransaction SendParentOrder(BfxParentOrder order, TimeSpan periodToExpire, BfTimeInForce timeInForce)
        {
            order.ApplyParameters(ProductCode, Convert.ToInt32(periodToExpire.TotalMinutes), timeInForce);
            var trans = new BfxParentOrderTransaction(this, order, OnOrderTransactionEvent);
            _ = trans.SendOrderRequestAsync();
            return trans;
        }

        internal void RegisterTransaction(BfxChildOrderTransaction tran)
        {
            _childOrderTransactions.AddOrUpdate(tran.MarketId, tran, (key, value) =>
            {
                Debug.WriteLine($"--Child transaction place holder found after order sent. {tran.MarketId}");
                if (!(value is BfxChildTransactionPlaceHolder placeHolder))
                {
                    throw new ApplicationException();
                }

                // ToList() prevents "Collection was modified; enumeration operation may not execute."
                placeHolder.ChildOrderEvents.ToList().ForEach(coe => tran.OnChildOrderEvent(coe));
                return tran;
            });
        }

        internal void RegisterTransaction(BfxParentOrderTransaction tran)
        {
            _parentOrderTransactions.AddOrUpdate(tran.MarketId, tran, (key, value) =>
            {
                Debug.WriteLine($"--Parent transaction place holder found after order sent. {tran.MarketId}");
                if (!(value is BfxParentTransactionPlaceHolder placeHolder))
                {
                    throw new ApplicationException();
                }
                foreach (var poe in placeHolder.ParentOrderEvents)
                {
                    tran.OnParentOrderEvent(poe);
                    if (poe.EventType == BfOrderEventType.Trigger)
                    {
                        var childTran = new BfxChildOrderTransaction(this, (BfxChildOrder)tran.Order.Children[poe.ChildOrderIndex - 1], tran, OnOrderTransactionEvent);
                        RegisterTransaction(childTran);
                    }
                }
                return tran;
            });
        }

        void OnOrderTransactionEvent(object sender, BfxOrderTransactionEventArgs ev)
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
