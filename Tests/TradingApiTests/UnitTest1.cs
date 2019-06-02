//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Financial.Extensions;
using BitFlyerDotNet.LightningApi;
using BitFlyerDotNet.Trading;

namespace TradingApiTests
{
    [TestClass]
    public class UnitTest1
    {
        CompositeDisposable _disposables = new CompositeDisposable();

        const BfProductCode ProductCode = BfProductCode.FXBTCJPY;
        static string _key;
        static string _secret;
        BfTradingAccount _account;
        BfTradingMarket _market;

        [ClassInitialize]
        public static void Classinitialize(TestContext context)
        {
            // ApiKey and ApiSecret are defined in PrivateTest.runsettings
            // Should copy that file to any other directory such as desktop and fill them. 
            _key = context.Properties["ApiKey"].ToString();
            _secret = context.Properties["ApiSecret"].ToString();
        }

        [TestInitialize]
        public void Initialize()
        {
            _account = new BfTradingAccount();
            _account.Login(_key, _secret);
            _market = _account.GetMarket(ProductCode);
            _market.GetOrderBookSource();
            //_market.ChildOrderChanged += OnChildOrderChanged;
            _market.ChildOrderTransactionStateChanged += OnChildOrderTransactionChanged;
        }

        [TestMethod]
        public void TradingTickerTest()
        {
            new BfTradingMarketTickerSource(_market).Subscribe(ticker =>
            {
                Trace.WriteLine($"B:{ticker.BestBidPrice} A:{ticker.BestAskPrice} LTP:{ticker.LastTradedPrice} H:{ticker.MarketStatus}");
            }).AddTo(_disposables);

            Console.ReadLine();
        }

        [TestMethod]
        public void OrderCancelTest()
        {
        }

        void OnChildOrderChanged(BfxChildOrder order)
        {
            Console.WriteLine($"Order date:{order.OrderDate}");
            Console.WriteLine($"ID:{order.AcceptanceId}");
            Console.WriteLine("");
        }

        void OnChildOrderTransactionChanged(object sender, BfxChildOrderTransactionEventArgs args)
        {
            Console.WriteLine($"Requested time   :{args.State.RequestedTime}");
            Console.WriteLine($"Accepted time    :{args.State.AcceptedTime}");
            Console.WriteLine($"Confirmed time   :{args.State.OrderDate}");
            Console.WriteLine($"Ordering status  :{args.State.OrderingStatus}");
            Console.WriteLine($"Canceling status :{args.State.CancelingStatus}");
        }

        public void DifferentProductTest()
        {
        }

        public void StopOrderTest()
        {
        }

        public void StopLimitTest()
        {
        }

        public void TrailingStopTest()
        {

        }
    }
}
