<<<<<<< Updated upstream
//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fiats.Utils;
using BitFlyerDotNet.LightningApi;

namespace PublicApiTests
{
    [TestClass]
    public class UnitTest1
    {
        const BfProductCode ProductCode = BfProductCode.FXBTCJPY;
        BitFlyerClient _client;

        [TestInitialize]
        public void Initialize()
        {
           _client = new BitFlyerClient();
        }

        [TestMethod]
        public void GetBoard()
        {
            var resp = _client.GetBoard(ProductCode);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var board = resp.GetResult();
            Assert.IsTrue(board.Asks.Length > 0 && board.Bids.Length > 0);
        }

        [TestMethod]
        public void GetBoardState()
        {
            var resp = _client.GetBoardState(ProductCode);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var boardState = resp.GetResult();
            Assert.IsTrue(boardState.State != BfBoardState.Unknown);
            Assert.IsTrue(boardState.Health != BfBoardHealth.Unknown);
        }

        [TestMethod]
        public void GetChats()
        {
            var resp = _client.GetChats();
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var chats = resp.GetResult();
            Assert.IsTrue(chats.Length > 0);
        }

        [TestMethod]
        public void GetChatsFromDate()
        {
            // Get chats from 10 minuts before
            var resp = _client.GetChats(DateTime.UtcNow - TimeSpan.FromMinutes(10));
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var chats = resp.GetResult();
            Assert.IsTrue(chats.Length > 0);
        }

        [TestMethod]
        public void GetExchangeHealth()
        {
            var resp = _client.GetExchangeHealth(ProductCode);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var health = resp.GetResult();
            Assert.IsTrue(health.Status != BfBoardHealth.Unknown);
        }

        [TestMethod]
        public void GetExecutions()
        {
            // Default count = 100
            var resp = _client.GetExecutions(ProductCode);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var executions = resp.GetResult();
            Assert.IsTrue(executions.Length == 100);
        }

        [TestMethod]
        public void GetExecutions10()
        {
            var resp = _client.GetExecutions(ProductCode, 10);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var executions = resp.GetResult();
            Assert.IsTrue(executions.Length == 10);
        }

        [TestMethod]
        public void GetMarkets()
        {
            var resp = _client.GetMarkets();
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var markets = resp.GetResult();
            Assert.IsTrue(markets.Length > 0);
        }

        [TestMethod]
        public void GetTicker()
        {
            var resp = _client.GetTicker(ProductCode);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var ticker = resp.GetResult();
            Assert.AreEqual(ticker.ProductCode, ProductCode.ToEnumString());
        }
    }
}
=======
//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fiats.Utils;
using BitFlyerDotNet.LightningApi;

namespace PublicApiTests
{
    [TestClass]
    public class UnitTest1
    {
        const BfProductCode ProductCode = BfProductCode.FXBTCJPY;
        BitFlyerClient _client;

        [TestInitialize]
        public void Initialize()
        {
           _client = new BitFlyerClient();
        }

        [TestMethod]
        public void GetBoard()
        {
            var resp = _client.GetBoard(ProductCode);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var board = resp.GetResult();
            Assert.IsTrue(board.Asks.Length > 0 && board.Bids.Length > 0);
        }

        [TestMethod]
        public void GetBoardState()
        {
            var resp = _client.GetBoardState(ProductCode);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var boardState = resp.GetResult();
            Assert.IsTrue(boardState.State != BfBoardState.Unknown);
            Assert.IsTrue(boardState.Health != BfBoardHealth.Unknown);
        }

        [TestMethod]
        public void GetChats()
        {
            var resp = _client.GetChats();
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var chats = resp.GetResult();
            Assert.IsTrue(chats.Length > 0);
        }

        [TestMethod]
        public void GetChatsFromDate()
        {
            // Get chats from 10 minuts before
            var resp = _client.GetChats(DateTime.UtcNow - TimeSpan.FromMinutes(10));
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var chats = resp.GetResult();
            Assert.IsTrue(chats.Length > 0);
        }

        [TestMethod]
        public void GetExchangeHealth()
        {
            var resp = _client.GetExchangeHealth(ProductCode);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var health = resp.GetResult();
            Assert.IsTrue(health.Status != BfBoardHealth.Unknown);
        }

        [TestMethod]
        public void GetExecutions()
        {
            // Default count = 100
            var resp = _client.GetExecutions(ProductCode);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var executions = resp.GetResult();
            Assert.IsTrue(executions.Length == 100);
        }

        [TestMethod]
        public void GetExecutions10()
        {
            var resp = _client.GetExecutions(ProductCode, 10);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var executions = resp.GetResult();
            Assert.IsTrue(executions.Length == 10);
        }

        [TestMethod]
        public void GetMarkets()
        {
            var resp = _client.GetMarkets();
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var markets = resp.GetResult();
            Assert.IsTrue(markets.Length > 0);
        }

        [TestMethod]
        public void GetTicker()
        {
            var resp = _client.GetTicker(ProductCode);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var ticker = resp.GetResult();
            Assert.AreEqual(ticker.ProductCode, ProductCode.ToEnumString());
        }
    }
}
>>>>>>> Stashed changes
