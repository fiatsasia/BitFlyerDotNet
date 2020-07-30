//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
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
        public BfProductCode ProductCode { get; private set; }
        public BfxConfiguration Config { get; private set; }
        public BitFlyerClient Client => _account.Client;
        public RealtimeSourceFactory RealtimeSource => _account.RealtimeSource;
        public BfxTicker? Ticker { get; private set; }
        public DateTime ServerTime { get { return DateTime.UtcNow + (Ticker?.ServerTimeDiff ?? TimeSpan.Zero); } }

        public decimal MinimumOrderSize => ProductCode.MinimumOrderSize();

        // Trade informations
        public IEnumerable<BfxChildOrderTransaction> ChildOrderTransactions => _childOrderTransactions.Values;
        public IEnumerable<BfxChildOrder> ChildOrders => _childOrderTransactions.Values.Select(e => e.Order);
        public IEnumerable<BfxParentOrderTransaction> ParentOrderTransactions => _parentOrderTransactions.Values;
        public IEnumerable<BfxParentOrder> ParentOrders => _parentOrderTransactions.Values.Select(e => e.Order);
        public IReadOnlyList<BfPosition> Positions => _positions;

        // Events
        public event Action<BfxTicker>? TickerChanged;
        public event EventHandler<BfxOrderEventArgs>? OrderEvent;

        // Private properties
        CompositeDisposable _disposables = new CompositeDisposable();
        BfxAccount _account;
        ConcurrentDictionary<string, BfxChildOrderTransaction> _childOrderTransactions = new ConcurrentDictionary<string, BfxChildOrderTransaction>();
        ConcurrentDictionary<string, BfxParentOrderTransaction> _parentOrderTransactions = new ConcurrentDictionary<string, BfxParentOrderTransaction>();
        List<BfPosition> _positions = new List<BfPosition>();

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
            new BfxTickerSource(this).Subscribe(ticker =>
            {
                Ticker = ticker;
                TickerChanged?.Invoke(ticker);
            }).AddTo(_disposables);

            // Load child orders
            foreach (var child in _account.Client.GetChildOrders(ProductCode, BfOrderState.Active).GetMessage())
            {
                var execs = Client.GetPrivateExecutions(ProductCode, childOrderId: child.ChildOrderId).GetMessage();
                var order = new BfxChildOrder(ProductCode, child, execs);
                _childOrderTransactions.TryAdd(child.ChildOrderAcceptanceId, new BfxChildOrderTransaction(this, order));
            }

            // Load parent orders
            foreach (var parentOrder in _account.Client.GetParentOrders(ProductCode, orderState: BfOrderState.Active).GetMessage())
            {
                var detail = Client.GetParentOrder(ProductCode, parentOrderId: parentOrder.ParentOrderId).GetMessage();
                var xParentOrder = new BfxParentOrder(ProductCode, parentOrder, detail);
                var parentTran = new BfxParentOrderTransaction(this, xParentOrder);

                var childOrders = Client.GetChildOrders(ProductCode, parentOrderId: parentOrder.ParentOrderId).GetMessage();
                foreach (var childOrder in childOrders)
                {
                    var xChildOrder = new BfxChildOrder(ProductCode, childOrder);
                    var childTran = new BfxChildOrderTransaction(this, xChildOrder, parentTran);
                    _childOrderTransactions[childOrder.ChildOrderAcceptanceId] = childTran; // overrite
                }
                _parentOrderTransactions.TryAdd(parentOrder.ParentOrderAcceptanceId, parentTran);
            };

            // Get active positions from real market
            if (ProductCode == BfProductCode.FXBTCJPY)
            {
                var resp = _account.Client.GetPositions(ProductCode);
                if (!resp.IsError)
                {
                    _positions = new List<BfPosition>(resp.GetMessage());
                }
            }
        }

        internal void TryOpen()
        {
            if (Config == null)
            {
                Open();
            }
        }

        public IBfxOrderTransaction PlaceOrder(IBfxOrder order)
        {
            switch (order)
            {
                case BfxChildOrder child:
                    return SendChildOrder(child);

                case BfxParentOrder parent:
                    return SendParentOrder(parent);

                default:
                    throw new NotSupportedException();
            }
        }

#region Child order
        BfxChildOrderTransaction SendChildOrder(BfxChildOrder order)
        {
            TryOpen();
            var trans = new BfxChildOrderTransaction(this, order);
            var id = trans.SendOrderRequest();
            if (!_childOrderTransactions.TryAdd(id, trans))
            {
                throw new Exception();
            }
            trans.OrderTransactionEvent += OnChildOrderTransactionEvent;
            return trans;
        }

        // Called from Account - Assign child event to child or parent transaction
        internal void RedirectChildOrderEvents(BfChildOrderEvent coe)
        {
            if (_childOrderTransactions.TryGetValue(coe.ChildOrderAcceptanceId, out var tran))
            {
                if (tran.Parent == null)
                {
                    tran.OnChildOrderEvent(coe);
                }
                else
                {
                    tran.Parent.OnChildOrderEvent(coe);
                }
            }
        }

        // Called from ChildOrderTransaction
        void OnChildOrderTransactionEvent(object sender, BfxChildOrderTransactionEventArgs ev)
        {
            if (!(sender is BfxChildOrderTransaction tran))
            {
                throw new ArgumentException();
            }

            switch (ev.EventType)
            {
                case BfxOrderEventType.OrderSent:
                    _childOrderTransactions.TryAdd(ev.ChildOrderAcceptanceId, tran);
                    break;

                case BfxOrderEventType.OrderAccepted:
                    OrderEvent?.Invoke(this, new BfxSimpleOrderEventArgs { EventType = BfxOrderEventType.OrderAccepted, OrderEvent = ev.OrderEvent, Order = tran.Order });
                    break;

                case BfxOrderEventType.PartiallyExecuted:
                case BfxOrderEventType.Executed:
                    if (ProductCode == BfProductCode.FXBTCJPY)
                    {
                        Task.Run(() =>
                        {
                            var resp = _account.Client.GetPositions(ProductCode);
                            if (!resp.IsError)
                            {
                                _positions = new List<BfPosition>(resp.GetMessage());
                            }
                        });
                    }
                    break;
            }
        }
#endregion Child Order

#region Parent Order
        BfxParentOrderTransaction SendParentOrder(BfxParentOrder order)
        {
            TryOpen();
            var trans = new BfxParentOrderTransaction(this, order);
            var id = trans.SendOrderRequest();
            if (!_parentOrderTransactions.TryAdd(id, trans))
            {
                throw new Exception();
            }
            trans.OrderTransactionEvent += OnParentOrderTransactionEvent;
            return trans;
        }

        // Called from account
        internal void RedirectParentOrderEvents(BfParentOrderEvent poe)
        {
            if (!_parentOrderTransactions.TryGetValue(poe.ChildOrderAcceptanceId, out var tran))
            {
                return; // Children of parent orders (Probably never receive)
            }
            tran.OnParentOrderEvent(poe);
        }

        // Called from ParentOrderTransaction
        void OnParentOrderTransactionEvent(object sender, BfxParentOrderTransactionEventArgs ev)
        {
            // 1. ChildOrderEventを初めて処理した場合、_descendants にトランザクションを登録する。
            // 2.

            var tran = sender as BfxParentOrderTransaction;
            switch (ev.EventType)
            {
                case BfxOrderEventType.OrderSent:
                    // 1. _parentOrderTransactions に登録
                    break;

                case BfxOrderEventType.OrderAccepted:
                    // 1. クライアントに通知
                    break;

                case BfxOrderEventType.PartiallyExecuted:
                case BfxOrderEventType.Executed:
                    if (ProductCode == BfProductCode.FXBTCJPY)
                    {
                        Task.Run(() =>
                        {
                            var resp = _account.Client.GetPositions(ProductCode);
                            if (!resp.IsError)
                            {
                                _positions = new List<BfPosition>(resp.GetMessage());
                            }
                        });
                    }
                    break;
            }
        }
#endregion Parent Order
    }
}
