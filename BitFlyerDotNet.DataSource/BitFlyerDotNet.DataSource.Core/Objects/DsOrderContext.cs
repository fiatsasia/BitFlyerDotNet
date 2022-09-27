using System;
using System.Collections.Generic;
using System.Text;

namespace BitFlyerDotNet.DataSource;

internal class DsExecutionContext : BfExecutionContext
{
    [BsonId]
    public LiteDB.ObjectId Oid { get; set; }
}

internal class DsOrderContext : BfOrderContext
{
    [BsonId]
    public LiteDB.ObjectId Oid { get; set; }

    [BsonRef("orders")]
    internal List<DsOrderContext> Children { get; private set; }

    public override IReadOnlyList<BfOrderContext> GetChildren() => Children;

    [BsonRef("executions")]
    internal List<DsExecutionContext> Executions { get; private set; }

    public override IReadOnlyList<BfExecutionContext> GetExecutions() => Executions;

    public DsOrderContext(DsPrivateDataSource ds, string productCode) : base(ds, productCode) { }
}
