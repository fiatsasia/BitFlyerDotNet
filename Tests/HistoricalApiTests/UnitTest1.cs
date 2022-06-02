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
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitFlyerDotNet.LightningApi;
using BitFlyerDotNet.Historical;
using System.Threading.Tasks;

namespace HistoricalApiTests
{
    [TestClass]
    public class UnitTest1
    {
        static string _productCode;
        static string _key;
        static string _secret;
        static string _cacheDirectoryPath;
        static BitFlyerClient _client;
        static string _connStr;

        [TestInitialize]
        public void Initialize()
        {
        }

        [ClassInitialize]
        public static void Classinitialize(TestContext context)
        {
            _productCode = context.Properties["ProductCode"].ToString();
            _key = context.Properties["ApiKey"].ToString();
            _secret = context.Properties["ApiSecret"].ToString();
            _client = new BitFlyerClient(_key, _secret);
            _cacheDirectoryPath = context.Properties["CacheDirectoryPath"].ToString();
            _connStr = "data source=" + Path.Combine(_cacheDirectoryPath, "TradingApiTests.db3");
            //_connStr = "data source=" + Path.Combine(_cacheDirectoryPath, "account.db3");
        }

        [TestMethod]
        public void UpdateActiveOrders()
        {
            var account = new OrderSource(_client, _connStr, _productCode);
            account.UpdateActiveOrders();
        }

        [TestMethod]
        public void UpdateRecenrParentOrders()
        {
            var account = new OrderSource(_client, _connStr, _productCode);
            account.UpdateRecentParentOrders(DateTime.UtcNow - TimeSpan.FromDays(60));
        }

        [TestMethod]
        public void UpdateRecenrChildOrders()
        {
            var account = new OrderSource(_client, _connStr, _productCode);
            account.UpdateRecentChildOrders(DateTime.UtcNow - TimeSpan.FromDays(60));
        }

        [TestMethod]
        public void UpdateRecenrOrders()
        {
            var account = new OrderSource(_client, _connStr, _productCode);
            account.UpdateRecentOrders(DateTime.UtcNow - TimeSpan.FromDays(60));
        }

        [TestMethod]
        public void UpdateRecenrExecutions()
        {
            var account = new OrderSource(_client, _connStr, _productCode);
            account.UpdateRecentExecutions(DateTime.UtcNow - TimeSpan.FromDays(90));
        }

        [TestMethod]
        public void AccountInitialTest()
        {
            var account = new OrderSource(_client, _connStr, _productCode);
        }

        [TestMethod]
        public void InitializeExecutions()
        {
            var account = new OrderSource(_client, _connStr, _productCode);
            var execs = account.GetExecutions(_productCode, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow).ToList();
        }

        [TestMethod]
        public async Task InitializeBalances()
        {
            var account = new OrderSource(_client, _connStr, _productCode);
            var balances = await account.GetBalancesAsync(BfCurrencyCode.JPY, DateTime.UtcNow.AddYears(-3), DateTime.UtcNow).ToListAsync();
        }

        [TestMethod]
        public async Task InitializeCollaterals()
        {
            var account = new OrderSource(_client, _connStr, _productCode);
            var colls = await account.GetCollaterals(DateTime.UtcNow.AddYears(-3), DateTime.UtcNow).ToListAsync();
        }

        [TestMethod]
        public void HistoricalOhlcSource()
        {
#if false
            var cacheFolderBasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().ProcessName)
            );

            var frameSpan = TimeSpan.FromMinutes(5);
            var minutelyPeriod = DateTime.UtcNow.Round(frameSpan);
            var source = new HistoricalOhlcSource(BfProductCode.FXBTCJPY, frameSpan, minutelyPeriod, TimeSpan.FromHours(1), cacheFolderBasePath);
            source.Subscribe(ohlc =>
            {
                Console.WriteLine("{0:yyyy/MM/dd hh:mm:ss.fff} O:{1} H:{2} L:{3} C:{4} V:{5}", ohlc.Start.ToLocalTime(), ohlc.Open, ohlc.High, ohlc.Low, ohlc.Close, ohlc.Volume);
            });

            Console.WriteLine("Completed.");
#endif
        }

        [TestMethod]
        public void CacheUpdateRecentTest()
        {
            var client = new BitFlyerClient();
            var cacheFactory = new SqlServerCacheFactory(@"server=(local);Initial Catalog=bitflyer;Integrated Security=True");
            var cache = cacheFactory.CreateExecutionCache(BfProductCode.FX_BTC_JPY);
            var completed = new ManualResetEvent(false);
            var recentTime = DateTime.UtcNow;
            cache.UpdateRecents(client).Subscribe(exec =>
            {
                if (exec.ExecutedTime.Day != recentTime.Day)
                {
                    recentTime = exec.ExecutedTime;
                    Console.WriteLine("{0} Completed", recentTime.ToLocalTime().Date);
                }
            },
            () =>
            {
                completed.Set();
            });
            completed.WaitOne();
        }

        [TestMethod]
        public void CacheFillGapTest()
        {
            var client = new BitFlyerClient();
            var cacheFactory = new SqlServerCacheFactory(@"server=(local);Initial Catalog=bitflyer;Integrated Security=True");
            var cache = cacheFactory.CreateExecutionCache(BfProductCode.FX_BTC_JPY);
            var completed = new ManualResetEvent(false);
            var recentTime = DateTime.UtcNow;
            cache.FillGaps(client).Subscribe(exec =>
            {
                if (exec.ExecutedTime.Day != recentTime.Day)
                {
                    recentTime = exec.ExecutedTime;
                    Console.WriteLine("{0} Completed", recentTime.ToLocalTime().Date);
                }
            },
            () =>
            {
                completed.Set();
            });
            completed.WaitOne();
        }
    }

    static class DateTimeExtensions
    {
        public static DateTime Round(this DateTime dt, TimeSpan unit)
        {
            return new DateTime(dt.Ticks / unit.Ticks * unit.Ticks, dt.Kind);
        }
    }
}
