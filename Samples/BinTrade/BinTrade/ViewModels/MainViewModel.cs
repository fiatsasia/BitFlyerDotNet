//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Threading;
using System.ComponentModel;
using System.Reactive.Disposables;
using BitFlyerDotNet.LightningApi;
using BitFlyerDotNet.Trading;

namespace BinTrade.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        CompositeDisposable _disposables = new CompositeDisposable();
        SynchronizationContext _ctx;
        TradingAccount _account;

        decimal _bedAmount = 100.0m;
        decimal _size = 0.01m;

        public event PropertyChangedEventHandler PropertyChanged;

        public decimal LastTradedPrice { get; private set; }

        public decimal EntryPrice { get; private set; } = decimal.Zero;
        public decimal TargetPrice { get; private set; } = decimal.Zero;
        public decimal LossCutPrice { get; private set; } = decimal.Zero;
        public decimal TargetDifference { get { return Math.Abs(TargetPrice - EntryPrice); } }

        public string LogMessage { get; private set; } = "Initializing...";

        public bool IsOrderPlaceable { get; set; } = true;
        public bool IsPositionCloseable { get; set; } = false;

        IParentOrderTransaction _order;
        BfPosition _position;

        public MainViewModel()
        {
            _ctx = SynchronizationContext.Current;

            _account = (App.Current as App).Account;
            _account.OrderStatusChanged += OnOrderStatusChanged;
            _account.PositionStatusChanged += OnPositionStatusChanged;

            // Get and subscrive FXBTCJPY ticker
            _account.TickerReceived += tick =>
            {
                LastTradedPrice = tick.LastTradedPrice;
                _ctx.Post(_ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastTradedPrice))), null);
            };
        }

        private void OnOrderStatusChanged(OrderTransactionState status, IOrderTransaction transaction)
        {
            LogMessage = status.ToString();

            switch (status)
            {
                case OrderTransactionState.Executed: // All of IFDOCO child orders are done
                    EntryPrice = decimal.Zero;
                    TargetPrice = decimal.Zero;
                    LossCutPrice = decimal.Zero;

                    IsOrderPlaceable = true;
                    IsPositionCloseable = false;
                    break;
            }

            _ctx.Post(_ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty)), null);
        }

        private void OnPositionStatusChanged(BfPosition position, bool openedOrClosed)
        {
            if (openedOrClosed) // Opened
            {
                _position = position;
                IsPositionCloseable = true;
                LogMessage = "Position opened.";
            }
            _ctx.Post(_ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty)), null);
        }

        public void PutOrder()
        {
            IsOrderPlaceable = false;
            _ctx.Post(_ =>
            {
                EntryPrice = _account.AskPrice;
                TargetPrice = EntryPrice + _bedAmount / _size;
                LossCutPrice = EntryPrice - _bedAmount / _size;
                _order = TradeOrderFactory.CreateIFDOCO(_account,
                    new LimitPriceOrder(_account.ProductCode, BfTradeSide.Buy, _size, EntryPrice),
                    new StopOrder(_account.ProductCode, BfTradeSide.Sell, _size, TargetPrice),
                    new StopOrder(_account.ProductCode, BfTradeSide.Sell, _size, LossCutPrice)
                );
                _account.PlaceOrder(_order);
            }, null);
        }

        public void CallOrder()
        {
            IsOrderPlaceable = false;
            _ctx.Post(_ =>
            {
                EntryPrice = _account.BidPrice;
                TargetPrice = EntryPrice - _bedAmount / _size;
                LossCutPrice = EntryPrice + _bedAmount / _size;
                _order = TradeOrderFactory.CreateIFDOCO(_account,
                    new LimitPriceOrder(_account.ProductCode, BfTradeSide.Sell, _size, EntryPrice),
                    new StopOrder(_account.ProductCode, BfTradeSide.Buy, _size, TargetPrice),
                    new StopOrder(_account.ProductCode, BfTradeSide.Buy, _size, LossCutPrice)
                );
                _account.PlaceOrder(_order);
            }, null);
        }

        public void CloseTrade()
        {
        }
    }
}
