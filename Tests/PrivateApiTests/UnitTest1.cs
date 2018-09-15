//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fiats.Utils;
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

        [TestInitialize]
        public void Initialize()
        {
            _client = new BitFlyerClient(_key, _secret);
        }

        [ClassInitialize]
        public static void Classinitialize(TestContext context)
        {
            _productCode = Enum.Parse<BfProductCode>(context.Properties["ProductCode"].ToString());

            // ApiKey and ApiSecret are defined in PrivateTest.runsettings
            // Should copy that file to any other directory such as desktop and fill them. 
            _key = context.Properties["ApiKey"].ToString();
            _secret = context.Properties["ApiSecret"].ToString();
        }

        bool CheckUnauthorized(IBitFlyerResponse resp)
        {
            if (resp.IsUnauthorized)
            {
                Console.WriteLine("API is Unauthorized by user settings.");
            }
            return resp.IsUnauthorized;
        }

        [TestMethod]
        public void GetBalance()
        {
            var resp = _client.GetBalance();
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var balance = resp.GetResult();
            Assert.IsTrue(balance.Length > 0);
        }

        [TestMethod]
        public void GetBankAccounts()
        {
            var resp = _client.GetBankAccounts();
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var bankAccounts = resp.GetResult();
            Assert.IsTrue(bankAccounts.Length > 0);
        }

        [TestMethod]
        public void GetCoinAddresses()
        {
            var resp = _client.GetCoinAddresses();
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var coinAddresses = resp.GetResult();
            Assert.IsTrue(coinAddresses.Length > 0);
        }

        [TestMethod]
        public void GetDeposits()
        {
            var resp = _client.GetDeposits();
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var deposits = resp.GetResult();
            Console.WriteLine("Deposit count = {0}", deposits.Length);
        }

        [TestMethod]
        public void GetPermissions()
        {
            var resp = _client.GetPermissions();
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var permissions = resp.GetResult();
            Assert.IsTrue(permissions.Length > 0);
        }

        [TestMethod]
        public void GetTradingCommission()
        {
            var resp = _client.GetTradingCommission(_productCode);
            if (CheckUnauthorized(resp))
            {
                return;
            }
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var commission = resp.GetResult();
            Console.WriteLine("Commission rate = {0}", commission.CommissionRate);
        }
    }
}
