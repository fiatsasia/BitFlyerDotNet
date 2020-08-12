//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
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
        public BfxTicker? Ticker { get; private set; }
        public DateTime ServerTime { get { return DateTime.UtcNow + (Ticker?.ServerTimeDiff ?? TimeSpan.Zero); } }

        public decimal MinimumOrderSize => ProductCode.MinimumOrderSize();

        // Events
        public event Action<BfxTicker>? TickerChanged;
        public event EventHandler<BfxOrderTransactionEventArgs>? OrderTransactionEvent;

        // Private properties
        CompositeDisposable _disposables = new CompositeDisposable();
        BfxAccount _account;
        ConcurrentDictionary<string, IBfxChildOrderTransaction> _childOrderTransactions = new ConcurrentDictionary<string, IBfxChildOrderTransaction>();
        ConcurrentDictionary<string, IBfxParentOrderTransaction> _parentOrderTransactions = new ConcurrentDictionary<string, IBfxParentOrderTransaction>();

        public BfxMarket(BfxAccount account, BfProductCode productCode, BfxConfiguration config)
        {
            _account = account;
            ProductCode = productCode;
            Config = config ?? new BfxConfiguration();
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
            StartTicker();
            //LoadMarketInformations();
        }

        public void StartTicker()
        {
            new BfxTickerSource(this).Subscribe(ticker =>
            {
                Ticker = ticker;
                TickerChanged?.Invoke(ticker);
            }).AddTo(_disposables);
        }

        public void LoadMarketInformations()
        {
            // Load child orders
            foreach (var child in _account.Client.GetChildOrders(ProductCode, BfOrderState.Active).GetContent())
            {
                var execs = Client.GetPrivateExecutions(ProductCode, childOrderId: child.ChildOrderId).GetContent();
                var order = new BfxChildOrder(ProductCode, child, execs);
                _childOrderTransactions.TryAdd(child.ChildOrderAcceptanceId, new BfxChildOrderTransaction(this, order, OnOrderTransactionEvent));
            }

            // Load parent orders
            foreach (var parentOrder in _account.Client.GetParentOrders(ProductCode, orderState: BfOrderState.Active).GetContent())
            {
                var detail = Client.GetParentOrder(ProductCode, parentOrderId: parentOrder.ParentOrderId).GetContent();
                var xParentOrder = new BfxParentOrder(ProductCode, parentOrder, detail);
                var parentTran = new BfxParentOrderTransaction(this, xParentOrder, OnOrderTransactionEvent);

                var childOrders = Client.GetChildOrders(ProductCode, parentOrderId: parentOrder.ParentOrderId).GetContent();
                foreach (var childOrder in childOrders)
                {
                    var xChildOrder = new BfxChildOrder(ProductCode, childOrder);
                    var childTran = new BfxChildOrderTransaction(this, xChildOrder, parentTran, OnOrderTransactionEvent);
                    _childOrderTransactions[childOrder.ChildOrderAcceptanceId] = childTran; // overwrite
                }
                _parentOrderTransactions.TryAdd(parentOrder.ParentOrderAcceptanceId, parentTran);
            };
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

                if (childOrder.ChildOrderAcceptanceId != null)
                {
                    var childTran = new BfxChildOrderTransaction(this, childOrder, parentTran, OnOrderTransactionEvent);
                    _childOrderTransactions.AddOrUpdate(childOrder.ChildOrderAcceptanceId, childTran, (key, value) =>
                    {
                        Debug.WriteLine("--Child transaction place holder found and merged.");
                        if (!(value is BfxChildTransactionPlaceHolder placeHolder))
                        {
                            throw new ApplicationException();
                        }
                        placeHolder.ChildOrderEvents.ForEach(coe => parentTran.OnChildOrderEvent(coe));
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
            if (tran.Id == null)
            {
                throw new ApplicationException();
            }

            _childOrderTransactions.AddOrUpdate(tran.Id, tran, (key, value) =>
            {
                Debug.WriteLine("--Child transaction place holder found after order sent.");
                if (!(value is BfxChildTransactionPlaceHolder placeHolder))
                {
                    throw new ApplicationException();
                }
                placeHolder.ChildOrderEvents.ForEach(coe => tran.OnChildOrderEvent(coe));
                return tran;
            });
        }

        internal void RegisterTransaction(BfxParentOrderTransaction tran)
        {
            if (tran.Id == null)
            {
                throw new ApplicationException();
            }

            _parentOrderTransactions.AddOrUpdate(tran.Id, tran, (key, value) =>
            {
                Debug.WriteLine("--Parent transaction place holder found after order sent.");
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

            OrderTransactionEvent?.Invoke(sender, ev);
        }
    }
}
