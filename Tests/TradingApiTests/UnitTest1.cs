using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitFlyerDotNet.LightningApi;
using BitFlyerDotNet.Trading;

namespace TradingApiTests
{
    [TestClass]
    public class UnitTest1
    {
        const BfProductCode ProductCode = BfProductCode.FXBTCJPY;
        static string _key;
        static string _secret;
        BitFlyerClient _client;
        TradingAccount _account;

        const double Volume = 0.01;
        const int RetryMax = 3;
        static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(3);

        [TestInitialize]
        public void Initialize()
        {
            _client = new BitFlyerClient(_key, _secret);
            _account = new TradingAccount(BfProductCode.FXBTCJPY);
            _account.OrderStatusChanged += OnOrderStatusChanged;
        }

        [TestMethod]
        public async void MarketPriceOrderTest1()
        {
            var order = TradeOrderFactory.CreateMarketPriceOrder(_account, BfTradeSide.Buy, Volume);

            if (!await _account.PlaceOrder(order))
            {
                switch (order.TransactionStatus)
                {
                    case OrderTransactionState.OrderFailed:
                        break;

                    default:
                        break;
                }
            }
        }

        [TestMethod]
        public async void IFDOrderTest1()
        {
            var firstOrder = new MarketPriceOrder(ProductCode, BfTradeSide.Buy, Volume);
            var secondOrder = new TrailingStopOrder(ProductCode, BfTradeSide.Sell, Volume, 10000.0);
            var order = TradeOrderFactory.CreateIFD(_account, firstOrder, secondOrder);
            if (!await _account.PlaceOrder(order, RetryMax, RetryInterval))
            {
                switch (order.TransactionStatus)
                {
                    case OrderTransactionState.OrderFailed:
                        break;

                    default:
                        break;
                }
            }
        }

        void OnOrderStatusChanged(OrderTransactionState status, IOrderTransaction transaction)
        {

        }
    }
}
