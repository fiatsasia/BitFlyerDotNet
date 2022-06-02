//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using BitFlyerDotNet.LightningApi;
using System.Threading.Tasks;

namespace PrivateApiTests
{
    [TestClass]
    public class UnitTest1
    {
        static string _productCode;
        static string _key;
        static string _secret;
        BitFlyerClient _client;
        string _requestJson;
        bool _enableSendOrder = false;

        const string DummyChildOrderAcceptanceId = "JRF20200725-070531-482865";
        const string DummyChildOrderId = "JFX20200725-070531-213381F";
        const string DummyParentOrderAcceptanceId = "JRF20200725-073619-718102";
        const string DummyParentOrderId = "JCP20200725-073619-498654";

        [ClassInitialize]
        public static void Classinitialize(TestContext context)
        {
            _productCode = context.Properties["ProductCode"].ToString();

            // ApiKey and ApiSecret are defined in PrivateTest.runsettings
            // Should copy that file to any other directory such as desktop and fill them. 
            _key = context.Properties["ApiKey"].ToString();
            _secret = context.Properties["ApiSecret"].ToString();
        }

        [TestInitialize]
        public void Initialize()
        {
            _client = new BitFlyerClient(_key, _secret);
            _client.ConfirmCallback = (apiName, json) =>
            {
                var jobject = JsonConvert.DeserializeObject(json);
                json = JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings);
                _requestJson = $"{apiName}: " + Environment.NewLine + json;

                return _enableSendOrder; // Disable send order to market
            };
        }

        string GetRequestJson()
        {
            return _requestJson;
        }

        void EnableSendOrder(bool enable)
        {
            _enableSendOrder = enable;
        }

        void Dump(BitFlyerResponse resp)
        {
            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        void Dump(BfChildOrderStatus order)
        {
            Console.WriteLine($"{order.PagingId} {order.ChildOrderDate} {order.ChildOrderType} {order.ChildOrderState}");
        }

        [TestMethod]
        public async Task CancelAllChildOrders()
        {
            EnableSendOrder(true);
            try
            {
                var resp = await _client.CancelAllChildOrdersAsync(_productCode, CancellationToken.None);
                Assert.IsTrue(resp.IsOk); // Response message is nothing
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task CancelChildOrderByAcceptanceId()
        {
            throw new NotSupportedException();

            EnableSendOrder(false);
            var resp = await _client.CancelChildOrderAsync(_productCode, null, DummyChildOrderAcceptanceId, CancellationToken.None);
            Assert.IsTrue(resp.IsOk);
            Console.WriteLine(GetRequestJson());
        }

        [TestMethod]
        public async Task CancelChildOrderById()
        {
            throw new NotSupportedException();

            EnableSendOrder(false);
            var resp = await _client.CancelChildOrderAsync(_productCode, DummyChildOrderId, null, CancellationToken.None);
            Assert.IsTrue(resp.IsOk); // Order force canceled
            Console.WriteLine(GetRequestJson());
        }

        [TestMethod]
        public async Task CancelParentOrderByAcceptanceId()
        {
            throw new NotSupportedException();

            EnableSendOrder(false);
            var resp = await _client.CancelParentOrderAsync(_productCode, null, DummyParentOrderAcceptanceId, CancellationToken.None);
            Assert.IsTrue(resp.IsOk); // Order force canceled
            Console.WriteLine(GetRequestJson());
        }

        [TestMethod]
        public async Task CancelParentOrderById()
        {
            throw new NotSupportedException();

            EnableSendOrder(false);
            var resp = await _client.CancelParentOrderAsync(_productCode, DummyParentOrderId, null, CancellationToken.None);
            Assert.IsTrue(resp.IsOk); // Order force canceled
            Console.WriteLine(GetRequestJson());
        }

        [TestMethod]
        public async Task GetBalance()
        {
            try
            {
                var resp = await _client.GetBalanceAsync(CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetBalanceHistory()
        {
            try
            {
                var resp = await _client.GetBalanceHistoryAsync(BfCurrencyCode.JPY, 5, 0, 0, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetBankAccounts()
        {
            try
            {
                var resp = await _client.GetBankAccountsAsync(CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetChildOrders()
        {
            try
            {
                var resp = await _client.GetChildOrdersAsync(_productCode, BfOrderState.Unknown, 5, 0, 0, null, null, null, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetActiveChildOrders()
        {
            try
            {
                var resp = await _client.GetChildOrdersAsync(_productCode, BfOrderState.Active, 5, 0, 0, null, null, null, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetCompletedChildOrders()
        {
            try
            {
                var resp = await _client.GetChildOrdersAsync(_productCode, BfOrderState.Completed, 5, 0, 0, null, null, null, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetCanceledChildOrders()
        {
            try
            {
                var resp = await _client.GetChildOrdersAsync(_productCode, BfOrderState.Canceled, 5, 0, 0, null, null, null, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetExpiredChildOrders()
        {
            try
            {
                var resp = await _client.GetChildOrdersAsync(_productCode, BfOrderState.Expired, 5, 0, 0, null, null, null, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetRejectedChildOrders()
        {
            try
            {
                var resp = await _client.GetChildOrdersAsync(_productCode, BfOrderState.Rejected, 5, 0, 0, null, null, null, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetRecentChildOrders()
        {
            try
            {
                var orders = _client.GetChildOrdersAsync(_productCode, DateTime.UtcNow - TimeSpan.FromDays(60));
                await foreach (var order in orders)
                {
                    Dump(order);
                }
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetAddresses()
        {
            try
            {
                var resp = await _client.GetAddressesAsync(CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetCoinIns()
        {
            try
            {
                var resp = await _client.GetCoinInsAsync(0, 0, 0, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetCoinOuts()
        {
            try
            {
                var resp = await _client.GetCoinOutsAsync(0, 0, 0, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetCollateral()
        {
            try
            {
                var resp = await _client.GetCollateralAsync(CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetCollateralAccounts()
        {
            try
            {
                var resp = await _client.GetCollateralAccountsAsync(CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetCollateralHistory()
        {
            try
            {
                var resp = await _client.GetCollateralHistoryAsync(5, 0, 0, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetDeposits()
        {
            try
            {
                var resp = await _client.GetDepositsAsync(0, 0, 0, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetParentOrderDetail()
        {
            var order = (await _client.GetParentOrdersAsync(_productCode, BfOrderState.Completed, 1))[0];
            var parentOrderId = order.ParentOrderId;

            var resp = await _client.GetParentOrderAsync(_productCode, parentOrderId, null, CancellationToken.None);
            Assert.IsFalse(resp.IsUnauthorized, "Permission denied");
            Assert.IsFalse(resp.IsError, resp.ErrorMessage);
            Dump(resp);
            var content = resp.GetContent();
        }

        // 2020/07/27
        // GetParentOrders API is too slow or sometimes returns "Internal Server Error" if target period contains old order.
        // Probably old parent orders are stored another slow database.
        [TestMethod]
        [Timeout(30000)] // 30 seconds
        public async Task GetParentOrders()
        {
            try
            {
                var resp = await _client.GetParentOrdersAsync(_productCode, BfOrderState.Unknown, 0, 0, 0, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetActiveParentOrders()
        {
            try
            {
                var resp = await _client.GetParentOrdersAsync(_productCode, BfOrderState.Active, 0, 0, 0, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetCompletedParentOrders()
        {
            try
            {
                var resp = await _client.GetParentOrdersAsync(_productCode, BfOrderState.Completed, 0, 0, 0, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        [Timeout(30000)] // 30 seconds
        public async Task GetCanceledParentOrders()
        {
            try
            {
                var resp = await _client.GetParentOrdersAsync(_productCode, BfOrderState.Canceled, 0, 0, 0, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetExpiredParentOrders()
        {
            try
            {
                var resp = await _client.GetParentOrdersAsync(_productCode, BfOrderState.Expired, 0, 0, 0, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetRejectedParentOrders()
        {
            try
            {
                var resp = await _client.GetParentOrdersAsync(_productCode, BfOrderState.Rejected, 0, 0, 0, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetPermissions()
        {
            try
            {
                var resp = await _client.GetPermissionsAsync(CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetPositions()
        {
            try
            {
                var resp = await _client.GetPositionsAsync(_productCode, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetExecutions()
        {
            try
            {
                //var childOrder = await _client.GetChildOrdersAsync(_productCode, BfOrderState.Completed, count: 1);
                //var resp = await _client.GetPrivateExecutionsAsync(_productCode, 1, 0, 0, childOrder[0].ChildOrderId, null, CancellationToken.None);
                var resp = await _client.GetPrivateExecutionsAsync(_productCode, 1, 0, 0, null, null, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetTradingCommission()
        {
            try
            {
                var resp = await _client.GetTradingCommissionAsync(_productCode, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task GetWithdrawals()
        {
            try
            {
                var resp = await _client.GetWithdrawalsAsync(null, 0, 0, 0, CancellationToken.None);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                Dump(resp);
                var content = resp.GetContent();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public async Task Withdraw()
        {
            EnableSendOrder(false);
            var resp = await _client.WithdrawAsync(BfCurrencyCode.JPY, bankAccountId: 1234, amount: 12000m, authenticationCode: "012345", CancellationToken.None);
            Assert.IsFalse(resp.IsUnauthorized, "Permission denied");
            Assert.IsTrue(resp.IsOk);
            Console.WriteLine(GetRequestJson());
        }

        [TestMethod]
        public async Task SendChildOrder()
        {
            throw new NotSupportedException();

            EnableSendOrder(false);
            var order = BfOrderFactory.Limit(_productCode, BfTradeSide.Buy, 100.0m, 1.0m);
            var resp = await _client.SendChildOrderAsync(order, CancellationToken.None);
            Assert.IsTrue(resp.IsOk);
            Console.WriteLine(GetRequestJson());
        }

        [TestMethod]
        public async Task SendParentOrder()
        {
            throw new NotSupportedException();

            EnableSendOrder(false);
            var order = BfOrderFactory.Stop(_productCode, BfTradeSide.Sell, 100000m, 0.1m);
            var resp = await _client.SendParentOrderAsync(order, CancellationToken.None);
            Assert.IsTrue(resp.IsOk);
            Console.WriteLine(GetRequestJson());
        }
    }
}
