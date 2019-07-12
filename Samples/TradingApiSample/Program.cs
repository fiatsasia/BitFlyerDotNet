﻿//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Reactive.Disposables;
using BitFlyerDotNet.LightningApi;
using BitFlyerDotNet.Trading;

namespace TradingApiSample
{
    partial class Program
    {
        static char GetCh(bool echo = true) { var ch = Char.ToUpper(Console.ReadKey(true).KeyChar); if (echo) Console.WriteLine(ch); return ch; }
        static CompositeDisposable _disposables = new CompositeDisposable();

        const BfProductCode ProductCode = BfProductCode.FXBTCJPY;
        static BfTradingAccount _account;
        static BfTradingMarket _market;

        // Pass key and secret from arguments.
        static void Main(string[] args)
        {
            // Set API ket and secret
            _account = new BfTradingAccount();
            if (args.Length == 2)
            {
                _account.Login(args[0], args[1]);
            }
            else
            {
                Console.Write("API Key   :"); var key = Console.ReadLine();
                Console.Write("API Secret:"); var secret = Console.ReadLine();
                if (!string.IsNullOrEmpty(key))
                {
                    _account.Login(key, secret);
                }
                else
                {
                    _account.Initialize();
                }
            }

            // Register event handlers
            _market = _account.GetMarket(ProductCode);
            _market.ChildOrderTransactionStateChanged += OnChildOrderTransactionStateChanged;
            _market.ChildOrderChanged += OnChildOrderChanged;
            _market.ParentOrderTransactionStateChanged += OnParentOrderTransactionStateChanged;
            _market.ParentOrderChanged += OnParentOrderChanged;
            _market.TickerChanged += OnTickerChanged;
            _market.PositionChanged += OnPositionChanged;

            while (true)
            {
                Console.WriteLine("======== Main menu");
                Console.WriteLine("1) Open market sample");
                Console.WriteLine("C) Child order samples");
                Console.WriteLine("P) Parent order samples");
                Console.WriteLine("");
                Console.WriteLine("Q) Quit sample");

                switch (GetCh())
                {
                    case '1':
                        OpenMarket();
                        break;

                    case 'C':
                        ChildOrderMain();
                        break;

                    case 'P':
                        ParentOrderMain();
                        break;

                    case 'Q':
                        _disposables.Dispose();
                        return;
                }
            }
        }

        // When open market, it starts market ticker publishing.
        static void OpenMarket()
        {
            var config = new BfTradingMarketConfiguration();
            config.PositionUpdateInterval = TimeSpan.FromSeconds(5);
            _market.Open(config);
        }

        // Called when position is added or removed.
        static void OnPositionChanged(BfPosition pos, bool addedOrRemoved)
        {
        }

        // Trading ticker is streaming from order book source and support SFD rate if market is FX_BTC_JPY
        static void OnTickerChanged(BfTradingMarketTicker ticker)
        {
            //Console.WriteLine($"{ticker.UpdatedTime.ToString("yyyy-MM-dd HH:mm:ss.fff")} {ticker.ServerTimeDiff}");
            //Console.WriteLine($"B:{ticker.BestBidPrice} A:{ticker.BestAskPrice} LTP:{ticker.LastTradedPrice} H:{ticker.MarketStatus} SFD:{ticker.SFDDifference}");
        }
    }
}