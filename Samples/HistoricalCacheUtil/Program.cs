//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using BitFlyerDotNet.LightningApi;
using BitFlyerDotNet.Historical;

namespace HistoricalCacheUtil
{
    class Program
    {
        static char GetCh() { var ch = Char.ToUpper(Console.ReadKey(true).KeyChar); Console.WriteLine(ch); return ch; }

        static void Main(string[] args)
        {
            var productCode = BfProductCode.FXBTCJPY;
            var client = new BitFlyerClient();
#if SQLSERVER
            var connStr = @"server=(local);Initial Catalog=bitflyer;Integrated Security=True";
            var cacheFactory = new SqlServerCacheFactory(connStr);
#elif SQLITE
            var folderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().ProcessName)
            );
            var cacheFactory = new SqliteCacheFactory(folderPath);
#else
            if (args.Length == 0)
            {
                DisplayUsage();
                return;
            }
            if (args.Length > 0 && args[0].ToUpper() != "/C")
            {
                productCode = Enum.Parse<BfProductCode>(args[0]);
                switch (args[1].ToUpper())
                {
                    case "SQLITE":
                        cacheFactory = new SqliteCacheFactory(args[2]);
                        break;

                    case "SQLSERVER":
                        cacheFactory = new SqlServerCacheFactory(args[2]);
                        break;

                    default:
                        DisplayUsage();
                        return;
                }

                switch (args[3].ToUpper())
                {
                    case "/U":
                        UpdateRecent(client, cacheFactory, productCode);
                        break;

                    default:
                        DisplayUsage();
                        return;
                }

                return;
            }
#endif
            Console.WriteLine("BitFlyerDotNet cache management utilities");
            Console.WriteLine("Copyright (C) 2017-2020 Fiats Inc.");

            try
            {
                while (true)
                {
                    Console.WriteLine();
                    Console.WriteLine("S)elect product");
                    Console.WriteLine("U)pdate recent historical executions in cache");
                    Console.WriteLine("F)ill fragmentations");
                    Console.WriteLine("A)dd specified count");
                    Console.WriteLine("R)ange updates");
                    Console.WriteLine("O)ptimize manage table");
                    //Console.WriteLine("I)mport SQLite cache");
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

                        case 'A':
                            {
                                Console.Write("Count : "); var count = int.Parse(Console.ReadLine());
                                Console.Write("Are you sure to start? (y/n)");
                                if (GetCh() == 'Y')
                                {
                                    StackExecutions(client, cacheFactory, productCode, count);
                                    Console.WriteLine("Completed.");
                                }
                            }
                            break;

                        case 'R':
                            {
                                Console.Write("Before : "); var before = int.Parse(Console.ReadLine());
                                Console.Write("After  : "); var after = int.Parse(Console.ReadLine());
                                Console.Write("Are you sure to start? (y/n)");
                                if (GetCh() == 'Y')
                                {
                                    GetExecutionsRange(client, cacheFactory, productCode, before, after);
                                    Console.WriteLine("Completed.");
                                }
                            }
                            break;

                        case 'O':
                            OptimizaManageTable(cacheFactory, productCode);
                            break;

                        case 'I':
                            //ImportCache(sqliteCacheFactory, sqlserverCacheFactory, productCode);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Hit Q key to exit.");
                while (GetCh() != 'Q') ;
            }
            finally
            {
            }
        }

        static void DisplayUsage()
        {
            Console.WriteLine(
@"Usage:
1) Command mode
dotnet CacheUtil.dll /C
2) Update FXBTCJPY recent data to SQL Server (database name = 'bitflyer')
dotnet CacheUtil.dll FXBTCJPY SQLSERVER ""server=(local);Initial Catalog=bitflyer;Integrated Security=True"" /U
3) Update FXBTCJPY recent data to SQLite
dotnet CacheUtil.dll FXBTCJPY SQLITE database-folder-path /U");
        }

        static void ExecuteArguments()
        {
        }

        static BfProductCode SelectProduct()
        {
            while (true)
            {
                Console.WriteLine("1)FXBTCJPY 2)BTCJPY 3)ETHJPY 4)BCHBTC 5)ETHBTC");
                switch (GetCh())
                {
                    case '1': return BfProductCode.FXBTCJPY;
                    case '2': return BfProductCode.BTCJPY;
                    case '3': return BfProductCode.ETHJPY;
                    case '4': return BfProductCode.BCHBTC;
                    case '5': return BfProductCode.ETHBTC;
                }
            }
        }

        static void ImportCache(ICacheFactory factorySqlite, ICacheFactory factorySqlserver, BfProductCode productCode)
        {
            var recordCount = 0;
            var sw = new Stopwatch();
            sw.Start();
            using (var cacheSqlite = factorySqlite.GetExecutionCache(productCode))
            using (var cacheSqlserver = factorySqlserver.GetExecutionCache(productCode))
            {
                cacheSqlserver.CommitCount = 1000000;
                foreach (var exec in cacheSqlite.GetBackwardExecutions())
                {
                    cacheSqlserver.Add(exec);
                    if ((++recordCount % 10000) == 0)
                    {
                        Console.WriteLine("{0} {1} Completed {2} Elapsed", exec.ExecutedTime.ToLocalTime(), recordCount, sw.Elapsed);
                    }
                }
                cacheSqlserver.SaveChanges();
            }
            sw.Stop();
        }

        static void OptimizaManageTable(ICacheFactory factory, BfProductCode productCode)
        {
            using (var cache = factory.GetExecutionCache(productCode)) { }
        }

        static void StackExecutions(BitFlyerClient client, ICacheFactory factory, BfProductCode productCode, int count)
        {
            using (var cache = factory.GetExecutionCache(productCode))
            {
                var recs = cache.GetManageTable();
                cache.CommitCount *= 100;
                var completed = new ManualResetEvent(false);
                var recordCount = 0;
                var sw = new Stopwatch();
                sw.Start();
                new HistoricalExecutionSource(client, productCode, recs[0].EndExecutionId + count, recs[0].EndExecutionId).Subscribe(exec =>
                {
                    cache.Add(exec);
                    if ((++recordCount % 10000) == 0)
                    {
                        Console.WriteLine("{0} {1} Completed {2} Elapsed", exec.ExecutedTime.ToLocalTime(), recordCount, sw.Elapsed);
                    }
                },
                () =>
                {
                    cache.SaveChanges();
                    sw.Stop();
                    completed.Set();
                });
                completed.WaitOne();
                cache.SaveChanges();
            }
        }

        static void GetExecutionsRange(BitFlyerClient client, ICacheFactory factory, BfProductCode productCode, int before, int after)
        {
            using (var cache = factory.GetExecutionCache(productCode))
            {
                cache.CommitCount *= 100;
                var completed = new ManualResetEvent(false);
                var recordCount = 0;
                var sw = new Stopwatch();
                sw.Start();
                new HistoricalExecutionSource(client, productCode, before, after).Subscribe(exec =>
                {
                    cache.Add(exec);
                    if ((++recordCount % 10000) == 0)
                    {
                        Console.WriteLine("{0} {1} Completed {2} Elapsed", exec.ExecutedTime.ToLocalTime(), recordCount, sw.Elapsed);
                    }
                },
                () =>
                {
                    cache.SaveChanges();
                    sw.Stop();
                    completed.Set();
                });
                completed.WaitOne();
                cache.SaveChanges();
            }
        }

        static void UpdateRecent(BitFlyerClient client, ICacheFactory factory, BfProductCode productCode)
        {
            using (var cache = factory.GetExecutionCache(productCode))
            {
                cache.CommitCount *= 100;
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

        static void FillGaps(BitFlyerClient client, ICacheFactory factory, BfProductCode productCode)
        {
            using (var cache = factory.GetExecutionCache(productCode))
            {
                cache.CommitCount *= 100;
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
                () =>
                {
                    cache.SaveChanges();
                    sw.Stop();
                    completed.Set();
                });
                completed.WaitOne();
            }
        }
    }
}
