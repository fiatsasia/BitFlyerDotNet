using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitFlyerDotNet.LightningApi;
using BitFlyerDotNet.Historical;
using Fiats.Utils;

namespace HistoricalApiTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestInitialize]
        public void Initialize()
        {
        }

        [ClassInitialize]
        public static void Classinitialize(TestContext context)
        {
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
        }

        [TestMethod]
        public void DbCreationTest()
        {
            var cacheFolderBasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().ProcessName)
            );

            var ctx = new ExecutionMinuteMarketDbContext(BfProductCode.FXBTCJPY, cacheFolderBasePath, "MARKER");
        }
    }
}
