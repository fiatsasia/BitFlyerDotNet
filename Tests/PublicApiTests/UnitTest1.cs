//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using BitFlyerDotNet.LightningApi;

namespace PublicApiTests
{
    [TestClass]
    public class UnitTest1
    {
        const string ProductCode = BfProductCode.FX_BTC_JPY;
        BitFlyerClient _client;

        [TestInitialize]
        public void Initialize()
        {
           _client = new BitFlyerClient();
        }

        [TestMethod]
        public async Task GetBoard()
        {
            var resp = await _client.GetBoardAsync(ProductCode, CancellationToken.None);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public async Task GetBoardState()
        {
            var resp = await _client.GetBoardStateAsync(ProductCode, CancellationToken.None);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public async Task GetChats()
        {
            // Until 5 minutes before
            var resp = await _client.GetChatsAsync(CancellationToken.None);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public async Task GetChatsUsa()
        {
            // Until 5 minutes before
            var resp = await _client.GetChatsUsaAsync(DateTime.UtcNow - TimeSpan.FromMinutes(5), CancellationToken.None);
            Assert.IsFalse(resp.IsError); // Usually empty

            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public async Task GetChatsEu()
        {
            // Until 5 minutes before
            var resp = await _client.GetChatsEuAsync(DateTime.UtcNow - TimeSpan.FromMinutes(5), CancellationToken.None);
            Assert.IsFalse(resp.IsError); // Usually empty

            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public async Task GetExecutions()
        {
            var resp = await _client.GetExecutionsAsync(ProductCode, 0, 0, 0, CancellationToken.None);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public async Task GetHealth()
        {
            var resp = await _client.GetHealthAsync(ProductCode, CancellationToken.None);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public async Task GetMarkets()
        {
            var resp = await _client.GetMarketsAsync(CancellationToken.None);
            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public async Task GetMarketsUsa()
        {
            var resp = await _client.GetMarketsUsaAsync(CancellationToken.None);
            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public async Task GetMarketsEu()
        {
            var resp = await _client.GetMarketsEuAsync(CancellationToken.None);
            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public async Task GetTicker()
        {
            var resp = await _client.GetTickerAsync(BfProductCode.FX_BTC_JPY, CancellationToken.None);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }

        [TestMethod]
        public async Task GetCorporateLeverage()
        {
            var resp = await _client.GetCorporateLeverageAsync(CancellationToken.None);
            var jobject = JsonConvert.DeserializeObject(resp.Json);
            Console.WriteLine(JsonConvert.SerializeObject(jobject, Formatting.Indented, BitFlyerClient.JsonSerializeSettings));
        }
    }
}
