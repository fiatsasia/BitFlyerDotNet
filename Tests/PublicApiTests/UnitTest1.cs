//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Linq;
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
            Console.WriteLine("{0}", board.MidPrice);
            for (int i = 0; i < Math.Min(board.Asks.Length, board.Bids.Length); i++)
            {
                Console.WriteLine("Ask {0} {1} Bid {2} {3}", board.Asks[i].Price, board.Asks[i].Size, board.Bids[i].Price, board.Bids[i].Size);
            }
        }

        [TestMethod]
        public void GetBoardState()
        {
            var resp = _client.GetBoardState(ProductCode);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var boardState = resp.GetResult();
            Console.WriteLine("Health:{0} State:{1}", boardState.Health, boardState.State);
        }

        [TestMethod]
        public void GetChats()
        {
            // Until 5 minutes before
            var resp = _client.GetChats(DateTime.UtcNow - TimeSpan.FromMinutes(5));
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var chats = resp.GetResult();
            chats.ForEach(chat => { Console.WriteLine("{0} {1} {2}", chat.Nickname, chat.Message, chat.Date.ToLocalTime()); });
        }

        [TestMethod]
        public void GetExchangeHealth()
        {
            var resp = _client.GetExchangeHealth(ProductCode);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var health = resp.GetResult();
            Console.WriteLine("{0}", health.Status);
        }

        [TestMethod]
        public void GetExecutions()
        {
            var resp = _client.GetExecutions(ProductCode);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var execs = resp.GetResult();
            execs.ForEach(exec =>
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5}",
                    exec.ExecutionId,
                    exec.Side,
                    exec.Price,
                    exec.Size,
                    exec.ExecutedTime.ToLocalTime(),
                    exec.ChildOrderAcceptanceId
                );
            });
        }

        [TestMethod]
        public void GetMarkets()
        {
            var resp = _client.GetMarkets();
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var markets = resp.GetResult();
            markets.ForEach(market => { Console.WriteLine("{0} {1}", market.ProductCode, market.Alias); });
        }

        [TestMethod]
        public void GetTicker()
        {
            var resp = _client.GetTicker(ProductCode);
            Assert.IsFalse(resp.IsErrorOrEmpty);

            var ticker = resp.GetResult();
            Console.WriteLine("{0} {1} {2} {3}",
                ticker.ProductCode,
                ticker.Timestamp.ToLocalTime(),
                ticker.BestAsk,
                ticker.BestBid
            );
        }
    }
}
