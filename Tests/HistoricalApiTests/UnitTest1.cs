//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Threading;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitFlyerDotNet.LightningApi;
using BitFlyerDotNet.Historical;

namespace HistoricalApiTests
{
    [TestClass]
    public class UnitTest1
    {
        static string _cacheDirectoryPath;

        [TestInitialize]
        public void Initialize()
        {
        }

        [ClassInitialize]
        public static void Classinitialize(TestContext context)
        {
            _cacheDirectoryPath = context.Properties["CacheDirectoryPath"].ToString();
        }

        [TestMethod]
        public void CryptowatchTest()
        {
            var frameSpan = TimeSpan.FromMinutes(1);
            var nextMinutelyPeriod = (DateTime.UtcNow + frameSpan).Round(frameSpan);

            CryptowatchOhlcSource.Get(BfProductCode.FXBTCJPY, frameSpan, nextMinutelyPeriod, nextMinutelyPeriod - TimeSpan.FromMinutes(5))
            .ForEach(ohlc =>
            {
                Console.WriteLine("{0:yyyy/MM/dd hh:mm:ss.fff} O:{1} H:{2} L:{3} C:{4} V:{5}", ohlc.Start.ToLocalTime(), ohlc.Open, ohlc.High, ohlc.Low, ohlc.Close, ohlc.Volume);
            });
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
            var cache = cacheFactory.GetExecutionCache(BfProductCode.FXBTCJPY);
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
            var cache = cacheFactory.GetExecutionCache(BfProductCode.FXBTCJPY);
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
