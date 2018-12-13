//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All right reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Concurrent;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public interface ICacheFactory
    {
        IExecutionCache GetExecutionCache(BfProductCode productCode);
        IOhlcCache GetOhlcCache(BfProductCode productCode, TimeSpan frameSpan);
    }

    public class SqliteCacheFactory : ICacheFactory
    {
        string _cacheFolderPath;
        ConcurrentDictionary<BfProductCode, SqliteCacheDbContext> _cacheContext = new ConcurrentDictionary<BfProductCode, SqliteCacheDbContext>();

        public SqliteCacheFactory(string cacheFolderPath)
        {
            _cacheFolderPath = cacheFolderPath;
        }

        public IExecutionCache GetExecutionCache(BfProductCode productCode)
        {
            return new ExecutionCache(_cacheContext.GetOrAdd(productCode, _ => { return new SqliteCacheDbContext(_cacheFolderPath, productCode); }));
        }

        public IOhlcCache GetOhlcCache(BfProductCode productCode, TimeSpan frameSpan)
        {
            return new OhlcCache(_cacheContext.GetOrAdd(productCode, _ => { return new SqliteCacheDbContext(_cacheFolderPath, productCode); }), frameSpan);
        }
    }

    public class SqlServerCacheFactory : ICacheFactory
    {
        readonly string _connStr;
        ConcurrentDictionary<BfProductCode, SqlServerCacheDbContext> _cacheContext = new ConcurrentDictionary<BfProductCode, SqlServerCacheDbContext>();

        public SqlServerCacheFactory(string connStr)
        {
            _connStr = connStr;
        }

        public IExecutionCache GetExecutionCache(BfProductCode productCode)
        {
            return new ExecutionCache(_cacheContext.GetOrAdd(productCode, _ => { return new SqlServerCacheDbContext(_connStr, productCode); }));
        }

        public IOhlcCache GetOhlcCache(BfProductCode productCode, TimeSpan frameSpan)
        {
            return new OhlcCache(_cacheContext.GetOrAdd(productCode, _ => { return new SqlServerCacheDbContext(_connStr, productCode); }), frameSpan);
        }
    }
}
