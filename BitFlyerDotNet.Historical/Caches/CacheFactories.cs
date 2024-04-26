//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Historical;

public interface ICacheFactory
{
    IExecutionCache CreateExecutionCache(string productCode);
    ICacheDbContext CreateDbContext(string productCode);
}

public class SqliteCacheFactory : ICacheFactory
{
    string _cacheFolderPath;

    public SqliteCacheFactory(string cacheFolderPath)
    {
        _cacheFolderPath = cacheFolderPath;
    }

    public IExecutionCache CreateExecutionCache(string productCode)
    {
        return new ExecutionCache(new SqliteDbContext(_cacheFolderPath, productCode), productCode);
    }

    public ICacheDbContext CreateDbContext(string productCode)
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

    public IExecutionCache CreateExecutionCache(string productCode)
    {
        return new ExecutionCache(new SqlServerDbContext(_connStr, productCode), productCode);
    }

    public ICacheDbContext CreateDbContext(string productCode)
    {
        return new SqlServerDbContext(_connStr, productCode);
    }
}
