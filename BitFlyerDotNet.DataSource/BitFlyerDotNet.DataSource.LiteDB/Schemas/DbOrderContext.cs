using System;
using System.Collections.Generic;
using System.Text;

namespace BitFlyerDotNet.DataSource;

internal class DsExecutionContext : BdExecutionContext
{
    [BsonId]
    public LiteDB.ObjectId Oid { get; set; }
}

internal class DbOrderContext : BdOrderContext
{
    [BsonId]
    public LiteDB.ObjectId Oid { get; set; }

    [BsonRef("orders")]
    internal List<DbOrderContext> Children { get; private set; }

    public override IReadOnlyList<BdOrderContext> GetChildren() => Children;

    [BsonRef("executions")]
    internal List<DsExecutionContext> Executions { get; private set; }

    public override IReadOnlyList<BdExecutionContext> GetExecutions() => Executions;

    public DbOrderContext(LiteDbDataSource ds, string productCode) : base(ds, productCode) { }

    public DbOrderContext(LiteDbDataSource ds, string productCode, BfOrderType orderType) : base(ds, productCode, orderType) { }
}
