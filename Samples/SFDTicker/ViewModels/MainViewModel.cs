//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Windows.Media;

using System.Reactive.Disposables;
using Fiats.Utils;

using BitFlyerDotNet.LightningApi;

namespace SFDTicker.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        CompositeDisposable _disposables = new CompositeDisposable();
        SynchronizationContext _ctx;

        BitFlyerRealtimeSourceFactory _factory;
        BitFlyerClient _client;
        BfTicker _fxBtcJpyTickerTick;
        BfTicker _btcJpyTickerTick;
        Timer _serverStatusTimer;

        public event PropertyChangedEventHandler PropertyChanged;
        public double PriceFXBTCJPY { get; private set; }
        public double PriceBTCJPY { get; private set; }
        public double SFDVariance { get; private set; }
        public double SFDRate { get; private set; }

        public BfBoardHealth ExchangeStatus { get; private set; }
        public Brush ExchangeStatusColor
        {
            get
            {
                switch (ExchangeStatus)
                {
                    case BfBoardHealth.Busy:
                        return Brushes.LightCyan;

                    case BfBoardHealth.VeryBusy:
                        return Brushes.PaleGreen;

                    case BfBoardHealth.SuperBusy:
                        return Brushes.Gold;

                    case BfBoardHealth.NoOrder:
                    case BfBoardHealth.Stop:
                        return Brushes.Red;

                    default:
                        return Brushes.White;
                }
            }
        }

        public MainViewModel()
        {
            _ctx = SynchronizationContext.Current;
            _factory = new BitFlyerRealtimeSourceFactory(BfRealtimeSourceKind.PubNub);
            _client = new BitFlyerClient();

            // Get and subscrive FXBTCJPY ticker
            _factory.GetTickerSource(BfProductCode.FXBTCJPY).Subscribe(tick =>
            {
                _fxBtcJpyTickerTick = tick;
                PriceFXBTCJPY = _fxBtcJpyTickerTick.LastTradedPrice;
                if (_fxBtcJpyTickerTick != default(BfTicker) && _btcJpyTickerTick != default(BfTicker))
                {
                    SFDVariance = (PriceFXBTCJPY - PriceBTCJPY) / PriceBTCJPY;
                    SFDRate = CalculateSfdRate(Math.Abs(SFDVariance));
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
                    SFDVariance = (PriceFXBTCJPY - PriceBTCJPY) / PriceBTCJPY;
                    SFDRate = CalculateSfdRate(Math.Abs(SFDVariance));
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

        double CalculateSfdRate(double variance)
        {
            var sfd = 0.0;
            if (Math.Abs(variance) < 0.05)
            {
                return 0.0; // SFD is none
            }
            else if (Math.Abs(variance) < 0.1) // 5% <= variance < 10%
            {
                sfd = 0.0025;
            }
            else if (Math.Abs(variance) < 0.15) // 10% <= variance < 15%
            {
                sfd = 0.005;
            }
            else if (Math.Abs(variance) < 0.20) // 15% <= variance < 20%
            {
                sfd = 0.01;
            }
            else // variance >= 20%
            {
                sfd = 0.02;
            }

            return sfd;
        }
    }
}
