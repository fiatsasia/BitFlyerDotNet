//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Financial.Extensions;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfTradingMarket : IDisposable
    {
        CompositeDisposable _disposables = new CompositeDisposable();

        public BfProductCode ProductCode { get; private set; }
        BfTradingAccount _account;
        public BfTradingMarketConfiguration Config { get; private set; }

        public BitFlyerClient Client => _account.Client;
        public RealtimeSourceFactory RealtimeSource => _account.RealtimeSource;
        public IObservable<BfOrderBook> GetOrderBookSource() => _account.RealtimeSource.GetOrderBookSource(ProductCode);
        //public IObservable<BfTradeTick>

        public BfTradingMarketTicker Ticker { get; private set; }
        public DateTime ServerTime { get { return DateTime.UtcNow + Ticker.ServerTimeDiff; } }
        public decimal BestAskPrice => Ticker.BestAskPrice;
        public decimal BestBidPrice => Ticker.BestBidPrice;

        ConcurrentBag<BfPosition> _positions = new ConcurrentBag<BfPosition>();
        public IEnumerable<BfPosition> Positions => _positions;
        ConcurrentBag<BfParentOrderDetail> _parentOrders = new ConcurrentBag<BfParentOrderDetail>();
        public IEnumerable<BfParentOrderDetail> ActiveParentOrders => _parentOrders;
        ConcurrentBag<BfChildOrder> _childOrders = new ConcurrentBag<BfChildOrder>();
        public IEnumerable<BfChildOrder> ActiveChildOrders => _childOrders;

        // Events
        public event EventHandler<BfxChildOrderTransactionEventArgs> ChildOrderTransactionStateChanged;
        public event EventHandler<BfxChildOrderEventArgs> ChildOrderChanged;
        public event EventHandler<BfxParentOrderTransactionEventArgs> ParentOrderTransactionStateChanged;
        public event EventHandler<BfxParentOrderEventArgs> ParentOrderChanged;

        public event Action<BfTradingMarketTicker> TickerChanged;
        public event Action<BfPosition, bool> PositionChanged;

        public readonly decimal MinimumOrderSize;

        public BfTradingMarket(BfTradingAccount account, BfProductCode productCode)
        {
            _account = account;
            ProductCode = productCode;

            switch (productCode)
            {
                case BfProductCode.FXBTCJPY:
                case BfProductCode.ETHBTC:
                case BfProductCode.BCHBTC:
                    MinimumOrderSize = 0.01m;
                    break;

                default:
                    MinimumOrderSize = 0.001m;
                    break;
            }
        }

        public bool IsOpened { get; private set; }
        public void Open(BfTradingMarketConfiguration config = null)
        {
            Config = config ?? new BfTradingMarketConfiguration();

            new BfTradingMarketTickerSource(this)
            .Subscribe(ticker =>
            {
                Ticker = ticker;
                TickerChanged?.Invoke(ticker);
            }).AddTo(_disposables);

            if (Client.IsPrivateApiEnabled)
            {
                // Get positions
                Observable.Timer(TimeSpan.FromSeconds(0), Config.PositionUpdateInterval).Subscribe(count => OnPositionUpdate()).AddTo(_disposables);

                // Get active orders
                Client.GetParentOrders(ProductCode, BfOrderState.Active).GetResult().ForEach(e =>
                {
                    _parentOrders.Add(Client.GetParentOrder(ProductCode, e.ParentOrderId).GetResult());
                });

                Client.GetChildOrders(ProductCode, BfOrderState.Active).GetResult().ForEach(e => _childOrders.Add(e));
            }

            IsOpened = true;
        }

        public void Dispose()
        {
            _disposables.Dispose();
            if (IsOpened)
            {
                while (!_positions.IsEmpty) _positions.TryTake(out BfPosition pos);
                while (!_parentOrders.IsEmpty) _parentOrders.TryTake(out BfParentOrderDetail po);
                while (!_childOrders.IsEmpty) _childOrders.TryTake(out BfChildOrder co);
            }
        }

        void OnChildOrderTransactionStateChanged(object sender, BfxChildOrderTransactionEventArgs args)
        {
            ChildOrderTransactionStateChanged?.Invoke(sender, args);
        }

        void OnChildOrderChanged(object sender, BfxChildOrderEventArgs args) // Called from BfTradingChildOrder
        {
            ChildOrderChanged?.Invoke(sender, args); // will call oberver.OnNext
        }

        void OnParentOrderTransactionStateChanged(object sender, BfxParentOrderTransactionEventArgs args)
        {
            ParentOrderTransactionStateChanged?.Invoke(sender, args);
        }

        void OnParentOrderChanged(object sender, BfxParentOrderEventArgs args)
        {
            ParentOrderChanged?.Invoke(sender, args);
        }

        public Task<BfxChildOrderTransaction> PlaceOrder(BfChildOrderRequest request, object tag = null)
        {
            DebugEx.Trace();
            return Task.Run(() =>
            {
                DebugEx.Trace();
                var trans = new BfxChildOrderTransaction(this, request);
                trans.StateChanged += OnChildOrderTransactionStateChanged;
                trans.OrderChanged += OnChildOrderChanged;
                trans.Tag = tag;

                for (var retry = 0; retry <= Config.OrderRetryMax; retry++)
                {
                    DebugEx.Trace();
                    if (!trans.IsOrderable())
                    {
                        DebugEx.Trace();
                        trans.Dispose();
                        return null; // order is unacceptable
                    }

                    if (trans.SendOrder())
                    {
                        DebugEx.Trace();
                        return trans;
                    }

                    DebugEx.Trace("Trying retry...");
                    Thread.Sleep(Config.OrderRetryInterval);
                }

                DebugEx.Trace();
                trans.Dispose();
                return null; // Retried out
            });
        }

        public Task<BfxParentOrderTransaction> PlaceOrder(BfParentOrderRequest request, object tag = null)
        {
            DebugEx.Trace();
            return Task.Run(() =>
            {
                DebugEx.Trace();
                var trans = new BfxParentOrderTransaction(this, request);
                trans.ChildOrderChanged += OnChildOrderChanged;
                trans.ParentOrderStateChanged += OnParentOrderTransactionStateChanged;
                trans.ParentOrderChanged += OnParentOrderChanged;
                trans.Tag = tag;

                for (var retry = 0; retry <= Config.OrderRetryMax; retry++)
                {
                    DebugEx.Trace();
                    if (!trans.IsOrderable())
                    {
                        DebugEx.Trace();
                        trans.Dispose();
                        return null; // order is unacceptable
                    }

                    if (trans.SendOrder())
                    {
                        DebugEx.Trace();
                        return trans;
                    }

                    DebugEx.Trace("Trying retry...");
                    Thread.Sleep(Config.OrderRetryInterval);
                }

                DebugEx.Trace();
                trans.Dispose();
                return null; // Retried out
            });
        }

        public void CancelOrder(IBfOrderTransaction tran)
        {
            // ToDo: リトライ処理
            tran.CancelOrder();
        }

        void OnPositionUpdate()
        {
            try
            {
                var positions = Client.GetPositions(ProductCode).GetResult();
                positions.Except(_positions).ForEach(e => PositionChanged?.Invoke(e, true));    // Notify opened positions
                _positions.Except(positions).ForEach(e => PositionChanged?.Invoke(e, false));   // Notify closed positions
                Interlocked.Exchange(ref _positions, new ConcurrentBag<BfPosition>(positions));
            }
            catch (Exception ex)
            {
                DebugEx.Trace(ex.Message);
            }
        }
    }
}
