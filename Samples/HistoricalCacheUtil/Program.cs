//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using BitFlyerDotNet.LightningApi;
using BitFlyerDotNet.Historical;

namespace HistoricalCacheUtil
{
    class Program
    {
        const int CommitCount = 500 * 500; // record/request * request limit count

        static char GetCh() { var ch = Char.ToUpper(Console.ReadKey(true).KeyChar); Console.WriteLine(ch); return ch; }

        static void Main(string[] args)
        {
            var productCode = "FX_BTC_JPY";
            var client = new BitFlyerClient();
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true)
                .Build();
            var connStr = config.GetConnectionString("bitflyer");
            var cacheFactory = new SqlServerCacheFactory(connStr);

            if (args.Length > 0)
            {
                productCode = args[0];
                UpdateRecent(client, cacheFactory, productCode);
                FillGaps(client, cacheFactory, productCode);
                GenerateOhlc(cacheFactory, productCode);
                return;
            }

            Console.WriteLine("BitFlyerDotNet cache management utilities");
            Console.WriteLine("Copyright (C) 2017-2022 Fiats Inc.");

            try
            {
                while (true)
                {
                    Console.WriteLine();
                    Console.WriteLine("S)elect product");
                    Console.WriteLine("U)pdate recent historical executions in cache");
                    Console.WriteLine("F)ill fragmentations");
                    Console.WriteLine("O)ptimize manage table");
                    Console.WriteLine("G)enerate OHLC");
                    Console.WriteLine();
                    Console.WriteLine("Hit Q key to exit.");

                    switch (GetCh())
                    {
                        case 'Q':
                            return;

                        case 'S':
                            productCode = SelectProduct();
                            break;

                        case 'U':
                            UpdateRecent(client, cacheFactory, productCode);
                            Console.WriteLine("Completed.");
                            break;

                        case 'F':
                            FillGaps(client, cacheFactory, productCode);
                            Console.WriteLine("Completed.");
                            break;

                        case 'O':
                            OptimizaManageTable(cacheFactory, productCode);
                            break;

                        case 'G':
                            GenerateOhlc(cacheFactory, productCode);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("Hit Q key to exit.");
                while (GetCh() != 'Q') ;
            }
            finally
            {
            }
        }

        static string SelectProduct()
        {
            while (true)
            {
                Console.WriteLine("1)FXBTCJPY 2)BTCJPY 3)ETHJPY 4)BCHBTC 5)ETHBTC");
                switch (GetCh())
                {
                    case '1': return "FX_BTC_JPY";
                    case '2': return "BTC_JPY";
                    case '3': return "ETH_JPY";
                    case '4': return "BCH_BTC";
                    case '5': return "ETH_BTC";
                }
            }
        }

        static void OptimizaManageTable(ICacheFactory factory, string productCode)
        {
            using (var cache = factory.CreateExecutionCache(productCode)) { }
        }

        static void UpdateRecent(BitFlyerClient client, ICacheFactory factory, string productCode)
        {
            using (var cache = factory.CreateExecutionCache(productCode))
            {
                cache.CommitCount = CommitCount;
                var completed = new ManualResetEvent(false);
                var recordCount = 0;
                var sw = new Stopwatch();
                sw.Start();
                cache.UpdateRecents(client).Subscribe(exec =>
                {
                    if ((++recordCount % 10000) == 0)
                    {
                        Console.WriteLine("{0} {1} Completed {2} Elapsed", exec.ExecutedTime.ToLocalTime(), recordCount, sw.Elapsed);
                    }
                    if (client.IsApiLimitReached)
                    {
                        cache.SaveChanges();
                    }
                },
                ex =>
                {
                    sw.Stop();
                    Console.WriteLine(ex);
                    completed.Set();
                },
                () =>
                {
                    cache.SaveChanges();
                    sw.Stop();
                    completed.Set();
                });
                completed.WaitOne();
            }
        }

        static void FillGaps(BitFlyerClient client, ICacheFactory factory, string productCode)
        {
            using (var cache = factory.CreateExecutionCache(productCode))
            {
                cache.CommitCount = CommitCount;
                var completed = new ManualResetEvent(false);
                var recordCount = 0;
                var sw = new Stopwatch();
                sw.Start();
                cache.FillGaps(client).Subscribe(exec =>
                {
                    if ((++recordCount % 10000) == 0)
                    {
                        Console.WriteLine("{0} {1} Completed {2} Elapsed", exec.ExecutedTime.ToLocalTime(), recordCount, sw.Elapsed);
                    }
                },
                ex =>
                {
                    sw.Stop();
                    Console.WriteLine(ex);
                    completed.Set();
                },
                () =>
                {
                    cache.SaveChanges();
                    sw.Stop();
                    completed.Set();
                });
                completed.WaitOne();
            }
        }

        static void GenerateOhlc(ICacheFactory factory, string productCode)
        {
            var frameSpan = TimeSpan.FromMinutes(1);
            using (var ctx = factory.CreateDbContext(productCode))
            {
                var lastExec = ctx.LastExecutionTime.Round(frameSpan);
                var startOhlc = ctx.LastOhlcTime;
                if (startOhlc == DateTime.MinValue)
                {
                    startOhlc = new DateTime(lastExec.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                }
                else
                {
                    startOhlc += frameSpan;
                }

                if (lastExec == startOhlc)
                {
                    Console.WriteLine("OHLC had already up to date.");
                    return;
                }

                var execs = ctx.Executions
                    .Where(e => e.ExecutedTime >= startOhlc && e.ExecutedTime < lastExec)
                    .OrderBy(e => e.ExecutedTime)
                    .ThenBy(e => e.ExecutionId);
                var ohlc = default(DbOhlc);
                foreach (var exec in execs)
                {
                    if (ohlc == default)
                    {
                        ohlc = new DbOhlc(frameSpan, exec);
                        continue;
                    }

                    var startNew = exec.ExecutedTime.Round(frameSpan);
                    if (ohlc.Start == startNew) // In same frame
                    {
                        ohlc.Update(exec);
                        continue;
                    }

                    // Frame changed
                    ctx.Add(ohlc);
                    if (ohlc.Start.Minute == 0 && ohlc.Start.Hour == 0)
                    {
                        Console.WriteLine(ohlc.Start);
                    }

                    if (ohlc.Start + frameSpan == startNew) // There aren't missing frames
                    {
                        ohlc = new DbOhlc(frameSpan, exec);
                        continue;
                    }

                    // Complements missing frames
                    while (true)
                    {
                        ohlc = DbOhlc.CreateMissingFrame(ohlc);
                        ctx.Add(ohlc);
                        if (ohlc.Start + frameSpan == startNew)
                        {
                            ohlc = new DbOhlc(frameSpan, exec);
                            break;
                        }
                    }
                }
                ctx.Add(ohlc);
                Console.WriteLine(ohlc.Start);
                ctx.SaveChanges();
            }
        }
    }
}
