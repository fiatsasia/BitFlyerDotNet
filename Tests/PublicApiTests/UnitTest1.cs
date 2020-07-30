//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
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

            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public void GetBoardState()
        {
            var resp = _client.GetBoardState(ProductCode);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public void GetChats()
        {
            // Until 5 minutes before
            var resp = _client.GetChats(DateTime.UtcNow - TimeSpan.FromMinutes(5));
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public void GetExecutions()
        {
            var resp = _client.GetExecutions(ProductCode);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public void GetMarketHealth()
        {
            var resp = _client.GetMarketHealth(ProductCode);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public void GetMarkets()
        {
            // If fails exception will be thrown
            var resp = _client.GetMarkets();
            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));

            resp = _client.GetMarketsUsa();
            jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));

            resp = _client.GetMarketsEu();
            jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public void GetTicker()
        {
            var resp = _client.GetTicker(BfProductCode.BTCJPYMAT3M);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }
    }
}
