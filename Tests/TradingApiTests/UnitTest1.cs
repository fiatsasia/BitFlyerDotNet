using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitFlyerDotNet.LightningApi;
using BitFlyerDotNet.Trading;

namespace TradingApiTests
{
    [TestClass]
    public class UnitTest1
    {
        static BfProductCode _productCode;
        static string _key;
        static string _secret;
        BitFlyerClient _client;
        TradeAccount _account;

        const double Volume = 0.01;
        const int RetryMax = 3;
        static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(3);

        [TestInitialize]
        public void Initialize()
        {
            _client = new BitFlyerClient(_key, _secret);
            _account = new TradeAccount(BfProductCode.FXBTCJPY);
            _account.OrderStatusChanged += OnOrderStatusChanged;
        }

        [TestMethod]
        public async void MarketPriceOrderTest1()
        {
            var order = _account.CreateMarketPriceOrder(BfTradeSide.Buy, Volume);

            if (!await _account.PlaceOrder(order, RetryMax, RetryInterval))
            {
                switch (order.Status)
                {
                    case BfTradeOrderState.OrderFailed:
                        break;

                    default:
                        break;
                }
            }
        }

        [TestMethod]
        public async void IFDOrderTest1()
        {
            var firstOrder = _account.CreateMarketPriceOrder(BfTradeSide.Buy, Volume);
            var secondOrder = _account.CreateMarketPriceOrder(BfTradeSide.Sell, Volume);
            var order = _account.CreateIFD(firstOrder, secondOrder);
            if (!await _account.PlaceOrder(order, RetryMax, RetryInterval))
            {
                switch (order.Status)
                {
                    case BfTradeOrderState.OrderFailed:
                        break;

                    default:
                        break;
                }
            }
        }

        void OnOrderStatusChanged(BfTradeOrderState status, IBfTradeOrder order)
        {

        }
    }
}
