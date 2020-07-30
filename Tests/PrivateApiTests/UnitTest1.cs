//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using BitFlyerDotNet.LightningApi;

namespace PrivateApiTests
{
    [TestClass]
    public class UnitTest1
    {
        static BfProductCode _productCode;
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
            _productCode = Enum.Parse<BfProductCode>(context.Properties["ProductCode"].ToString());

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

        void DumpResponse(IBitFlyerResponse resp)
        {
            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public void CancelAllChildOrders()
        {
            EnableSendOrder(true);
            try
            {
                var resp = _client.CancelAllChildOrders(_productCode);
                Assert.IsTrue(resp.IsOk); // Response message is nothing
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void CancelChildOrderByAcceptanceId()
        {
            EnableSendOrder(false);
            var resp = _client.CancelChildOrder(_productCode, childOrderAcceptanceId: DummyChildOrderAcceptanceId);
            Assert.IsTrue(resp.IsOk);
            Console.WriteLine(GetRequestJson());
        }

        [TestMethod]
        public void CancelChildOrderById()
        {
            EnableSendOrder(false);
            var resp = _client.CancelChildOrder(_productCode, childOrderId: DummyChildOrderId);
            Assert.IsTrue(resp.IsOk); // Order force canceled
            Console.WriteLine(GetRequestJson());
        }

        [TestMethod]
        public void CancelParentOrderByAcceptanceId()
        {
            EnableSendOrder(false);
            var resp = _client.CancelParentOrder(_productCode, parentOrderAcceptanceId: DummyParentOrderAcceptanceId);
            Assert.IsTrue(resp.IsOk); // Order force canceled
            Console.WriteLine(GetRequestJson());
        }

        [TestMethod]
        public void CancelParentOrderById()
        {
            EnableSendOrder(false);
            var resp = _client.CancelParentOrder(_productCode, parentOrderId: DummyParentOrderId);
            Assert.IsTrue(resp.IsOk); // Order force canceled
            Console.WriteLine(GetRequestJson());
        }

        [TestMethod]
        public void GetBalance()
        {
            try
            {
                var resp = _client.GetBalance();
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void GetBalanceHistory()
        {
            try
            {
                var resp = _client.GetBalanceHistory(BfCurrencyCode.JPY, count: 5);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void GetBankAccounts()
        {
            try
            {
                var resp = _client.GetBankAccounts();
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void GetChildOrders()
        {
            try
            {
                var resp = _client.GetChildOrders(_productCode, count: 5);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void GetChildCanceledOrders()
        {
            try
            {
                var resp = _client.GetChildOrders(_productCode, BfOrderState.Canceled, count: 5);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void GetChildExpiredOrders()
        {
            try
            {
                var resp = _client.GetChildOrders(_productCode, BfOrderState.Expired, count: 5);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void GetChildRejectedOrders()
        {
            try
            {
                var resp = _client.GetChildOrders(_productCode, BfOrderState.Rejected, count: 5);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void GetCoinAddresses()
        {
            try
            {
                var resp = _client.GetCoinAddresses();
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void GetCoinIns()
        {
            try
            {
                var resp = _client.GetCoinIns();
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void GetCoinOuts()
        {
            try
            {
                var resp = _client.GetCoinOuts();
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void GetCollateral()
        {
            try
            {
                var resp = _client.GetCollateral();
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void GetCollateralHistory()
        {
            try
            {
                var resp = _client.GetCollateralHistory(count: 5);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void GetDeposits()
        {
            try
            {
                var resp = _client.GetDeposits();
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        [Ignore]
        public void GetParentOrder()
        {
            var resp = _client.GetParentOrder(_productCode, parentOrderAcceptanceId: DummyParentOrderAcceptanceId);
            Assert.IsFalse(resp.IsUnauthorized, "Permission denied");
            Assert.IsFalse(resp.IsError, resp.ErrorMessage);
            DumpResponse(resp);
            var message = resp.GetMessage();
        }

        // 2020/07/27
        // GetParentOrders API is too slow or sometimes returns "Internal Server Error" if target period contains old order.
        // Probably old parent orders are stored another slow database.
        [TestMethod]
        [Timeout(30000)] // 30 seconds
        public void GetParentOrders()
        {
            try
            {
                var resp = _client.GetParentOrders(_productCode, count: 1);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void GetActiveParentOrders()
        {
            try
            {
                var resp = _client.GetParentOrders(_productCode, BfOrderState.Active);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void GetPermissions()
        {
            try
            {
                var resp = _client.GetPermissions();
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void GetPositions()
        {
            try
            {
                var resp = _client.GetPositions(_productCode);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void GetExecutions()
        {
            try
            {
                //var resp = _client.GetChildOrders(_productCode, BfOrderState.Completed, count: 1);

                var resp = _client.GetPrivateExecutions(_productCode, count: 5);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void GetTradingCommission()
        {
            try
            {
                var resp = _client.GetTradingCommission(_productCode);
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void GetWithdrawals()
        {
            try
            {
                var resp = _client.GetWithdrawals();
                Assert.IsFalse(resp.IsError, resp.ErrorMessage);
                DumpResponse(resp);
                var message = resp.GetMessage();
            }
            catch (BitFlyerUnauthorizedException) // Should enable from settings
            {
            }
        }

        [TestMethod]
        public void SendChildOrder()
        {
            EnableSendOrder(false);
            var resp = _client.SendChildOrder(_productCode, BfOrderType.Limit, BfTradeSide.Buy, 100.0m, 1.0m);
            Assert.IsTrue(resp.IsOk);
            Console.WriteLine(GetRequestJson());
        }

        [TestMethod]
        public void SendParentOrder()
        {
            EnableSendOrder(false);
            var order = BfParentOrderRequest.Stop(_productCode, BfTradeSide.Sell, 0.1m, 100000m);
            var resp = _client.SendParentOrder(order);
            Assert.IsTrue(resp.IsOk);
            Console.WriteLine(GetRequestJson());
        }

        [TestMethod]
        public void Withdraw()
        {
            EnableSendOrder(false);
            var resp = _client.Withdraw(BfCurrencyCode.JPY, bankAccountId: 1234, amount: 12000m, authenticationCode: "012345");
            Assert.IsFalse(resp.IsUnauthorized, "Permission denied");
            Assert.IsTrue(resp.IsOk);
            Console.WriteLine(GetRequestJson());
        }
    }
}
