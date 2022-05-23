using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using BitFlyerDotNet.LightningApi;
using System.Collections.Concurrent;

namespace BitFlyerDotNet.Trading
{
    public class BfxMarket2 : IDisposable
    {
        public event EventHandler<BfxOrderChangedEventArgs>? OrderChanged;

        public decimal BestAsk => _ticker.BestAsk;
        public decimal BestBid => _ticker.BestBid;
        public decimal LastTradedPrice => _ticker.LastTradedPrice;

        CompositeDisposable _disposables = new();
        BitFlyerClient _client;
        RealtimeSourceFactory _rts;
        BfTicker _ticker = new();
        BfxPositions _positions = new();
        ConcurrentDictionary<string, IBfxTransaction> _childOrderTransactions = new();
        ConcurrentDictionary<string, IBfxTransaction> _parentOrderTransactions = new();

        string _productCode;

        public BfxMarket2(BitFlyerClient client, RealtimeSourceFactory rts, string productCode)
        {
            _client = client;
            _rts = rts;
            _productCode = productCode;
        }

        public void Dispose()
        {
            _disposables.DisposeReverse();
        }

        public async Task InitializeAsync()
        {
            if (_disposables.Count > 0)
            {
                return; // already initialized
            }

            // Initialize ticker
            _ticker = (await _client.GetTickerAsync(_productCode)).GetContent();
            _rts.GetTickerSource(_productCode).Subscribe(ticker => { _ticker = ticker; }).AddTo(_disposables);

            // Load active positions from market
            if (_productCode == BfProductCodeEx.FX_BTC_JPY)
            {
                _positions.Update((await _client.GetPositionsAsync(BfProductCodeEx.FX_BTC_JPY)).GetContent());
            }

            // Load active parent orders, their children and executions.
            var parentOrders = await _client.GetActiveParentOrders(_productCode);
            foreach (var order in parentOrders)
            {
                var parentOrder = new BfxParentOrder(order);
                var txParent = new BfxParentTransaction(this, parentOrder);
                foreach (var childOrder in parentOrder.Children.Cast<BfxChildOrder>())
                {
                    _childOrderTransactions[childOrder.AcceptanceId] = new BfxChildTransaction(this, childOrder, txParent);
                }
                _parentOrderTransactions.TryAdd(order.AcceptanceId, txParent);
            }

            // Load active independent child orders and their executions
            var childOrders = await _client.GetActiveIndependentChildOrders(_productCode);
            foreach (var order in childOrders)
            {
                var child = new BfxChildOrder(order);
                _childOrderTransactions.TryAdd(order.AcceptanceId, new BfxChildTransaction(this, child));
            }
        }

        internal void OnParentOrderEvent(BfParentOrderEvent e)
        {
        }

        internal void OnChildOrderEvent(BfChildOrderEvent e)
        {
        }
    }
}
