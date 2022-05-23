//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
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
        const string ProductCode = "FX_BTC_JPY";
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
        public void GetHealth()
        {
            var resp = _client.GetHealth(ProductCode);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public void GetMarketsAll()
        {
            // If fails exception will be thrown
            var resp = _client.GetMarketsAll();
            foreach (var element in resp)
            {
                var jobject = JsonConvert.DeserializeObject(element.Json);
                Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
            }
        }

        [TestMethod]
        public void GetMarkets()
        {
            var resp = _client.GetMarkets();
            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public void GetMarketsUsa()
        {
            var resp = _client.GetMarketsUsa();
            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public void GetMarketsEu()
        {
            var resp = _client.GetMarketsEu();
            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public void GetTicker()
        {
            var resp = _client.GetTicker("FX_BTC_JPY");
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }
    }
}
