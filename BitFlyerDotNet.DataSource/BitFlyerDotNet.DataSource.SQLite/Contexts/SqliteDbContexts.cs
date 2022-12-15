//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System.Data.SQLite;

namespace BitFlyerDotNet.DataSource;

class SqliteDbContext : CacheDbContext, IDisposable
{
    // Parameters
    public string _productCode { get; }

    public SqliteDbContext(string connectionString, string productCode)
        : base(new SQLiteConnection(new SQLiteConnectionStringBuilder(connectionString).ToString()))
    {
        _productCode = productCode;
    }

    public override void Dispose()
    {
        base.Dispose();
    }

    public IEnumerable<DbExecution> GetExecutions(DateTime start, TimeSpan span)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<DbOhlc> GetOhlcs(TimeSpan frameSpan, DateTime start, TimeSpan span)
    {
        throw new NotImplementedException();
    }
}
