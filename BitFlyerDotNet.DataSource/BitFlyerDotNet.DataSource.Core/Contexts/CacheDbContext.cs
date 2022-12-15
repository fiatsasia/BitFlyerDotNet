//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.DataSource;

public abstract class CacheDbContext : IDisposable
{
    IDbConnection _conn;

    protected CacheDbContext(IDbConnection conn)
    {
        _conn = conn;
    }

    public virtual void Dispose()
    {
        _conn.Close();
    }

    public IEnumerable<DbExecution> GetExecutions(string productCode, DateTime start, TimeSpan span)
    {
        return _conn.Query<DbExecution>(
            $"SELECT * FROM {productCode}"
            + $" WHERE {nameof(DbExecution.ExecutedTime)}>='{start}' AND {nameof(DbExecution.ExecutedTime)}<='{start + span}'"
        );
    }

    public IEnumerable<DbOhlc> GetOhlcs(string productCode, TimeSpan frameSpan, DateTime start, TimeSpan span)
    {
        return _conn.Query<DbOhlc>(
            $"SELECT * FROM {productCode}"
            + $" WHERE {nameof(DbOhlc.Start)}>='{start}' AND {nameof(DbOhlc.Start)}<='{start + span}'"
        );
    }
}
