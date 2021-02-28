//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public interface ICacheFactory
    {
        IExecutionCache GetExecutionCache(BfProductCode productCode);
        ICacheDbContext GetDbContext(BfProductCode productCode);
    }

    public class SqliteCacheFactory : ICacheFactory
    {
        string _cacheFolderPath;

        public SqliteCacheFactory(string cacheFolderPath)
        {
            _cacheFolderPath = cacheFolderPath;
        }

        public IExecutionCache GetExecutionCache(BfProductCode productCode)
        {
            return new ExecutionCache(new SqliteDbContext(_cacheFolderPath, productCode), productCode);
        }

        public ICacheDbContext GetDbContext(BfProductCode productCode)
        {
            return new SqliteDbContext(_cacheFolderPath, productCode);
        }
    }

    public class SqlServerCacheFactory : ICacheFactory
    {
        readonly string _connStr;

        public SqlServerCacheFactory(string connStr)
        {
            _connStr = connStr;
        }

        public IExecutionCache GetExecutionCache(BfProductCode productCode)
        {
            return new ExecutionCache(new SqlServerDbContext(_connStr, productCode), productCode);
        }

        public ICacheDbContext GetDbContext(BfProductCode productCode)
        {
            return new SqlServerDbContext(_connStr, productCode);
        }
    }
}
