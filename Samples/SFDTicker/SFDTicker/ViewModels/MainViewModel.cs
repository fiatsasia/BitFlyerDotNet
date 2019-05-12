//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Threading;
using System.ComponentModel;
using System.Reactive.Disposables;
using Xamarin.Forms;
using Fiats.Utils;
using BitFlyerDotNet.LightningApi;

namespace SFDTicker.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        CompositeDisposable _disposables = new CompositeDisposable();
        SynchronizationContext _ctx;

        RealtimeSourceFactory _factory;
        BitFlyerClient _client;
        BfTicker _fxBtcJpyTickerTick;
        BfTicker _btcJpyTickerTick;
        Timer _serverStatusTimer;

        public event PropertyChangedEventHandler PropertyChanged;
        public decimal PriceFXBTCJPY { get; private set; }
        public decimal PriceBTCJPY { get; private set; }
        public decimal SFDDifference { get; private set; }
        public decimal SFDRate { get; private set; }

        public BfBoardHealth ExchangeStatus { get; private set; }
        public Color ExchangeStatusColor
        {
            get
            {
                switch (ExchangeStatus)
                {
                    case BfBoardHealth.Busy:
                        return Color.LightCyan;

                    case BfBoardHealth.VeryBusy:
                        return Color.PaleGreen;

                    case BfBoardHealth.SuperBusy:
                        return Color.Gold;

                    case BfBoardHealth.NoOrder:
                    case BfBoardHealth.Stop:
                        return Color.Red;

                    default:
                        return Color.White;
                }
            }
        }

        public MainViewModel()
        {
            _ctx = SynchronizationContext.Current;
            _factory = new RealtimeSourceFactory();
            _client = new BitFlyerClient();

            // Get and subscrive FXBTCJPY ticker
            _factory.GetTickerSource(BfProductCode.FXBTCJPY).Subscribe(tick =>
            {
                _fxBtcJpyTickerTick = tick;
                PriceFXBTCJPY = _fxBtcJpyTickerTick.LastTradedPrice;
                if (_fxBtcJpyTickerTick != default(BfTicker) && _btcJpyTickerTick != default(BfTicker))
                {
                    SFDDifference = (PriceFXBTCJPY - PriceBTCJPY) / PriceBTCJPY;
                    SFDRate = CalculateSfdRate(Math.Abs(SFDDifference));
                    _ctx.Post(_ =>
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
                    }, null);
                }
            }).AddTo(_disposables);

            // Get and subscrive BTCJPY ticker
            _factory.GetTickerSource(BfProductCode.BTCJPY).Subscribe(tick =>
            {
                _btcJpyTickerTick = tick;
                PriceBTCJPY = _btcJpyTickerTick.LastTradedPrice;
                if (_fxBtcJpyTickerTick != default(BfTicker) && _btcJpyTickerTick != default(BfTicker))
                {
                    SFDDifference = (PriceFXBTCJPY - PriceBTCJPY) / PriceBTCJPY;
                    SFDRate = CalculateSfdRate(Math.Abs(SFDDifference));
                    _ctx.Post(_ =>
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
                    }, null);
                }
            }).AddTo(_disposables);

            // Get exchange status by timer
            _serverStatusTimer = new Timer(state =>
            {
                var resp = _client.GetExchangeHealth(BfProductCode.FXBTCJPY);
                if (!resp.IsError)
                {
                    ExchangeStatus = resp.GetResult().Status;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExchangeStatus)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExchangeStatusColor)));
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        decimal CalculateSfdRate(decimal variance)
        {
            var sfd = decimal.Zero;
            if (Math.Abs(variance) < 0.05m)
            {
                return decimal.Zero; // SFD is none
            }
            else if (Math.Abs(variance) < 0.1m) // 5% <= variance < 10%
            {
                sfd = 0.0025m;
            }
            else if (Math.Abs(variance) < 0.15m) // 10% <= variance < 15%
            {
                sfd = 0.005m;
            }
            else if (Math.Abs(variance) < 0.20m) // 15% <= variance < 20%
            {
                sfd = 0.01m;
            }
            else // variance >= 20%
            {
                sfd = 0.02m;
            }

            return sfd;
        }
    }
}
