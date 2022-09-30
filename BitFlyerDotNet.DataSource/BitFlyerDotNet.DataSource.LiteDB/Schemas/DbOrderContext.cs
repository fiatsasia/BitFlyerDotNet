using System;
using System.Collections.Generic;
using System.Text;

namespace BitFlyerDotNet.DataSource;

internal class DsExecutionContext : BfExecutionContext
{
    [BsonId]
    public LiteDB.ObjectId Oid { get; set; }
}

internal class DbOrderContext : BfOrderContextBase
{
    [BsonId]
    public LiteDB.ObjectId Oid { get; set; }

    [BsonRef("orders")]
    internal List<DbOrderContext> Children { get; private set; }

    [BsonRef("executions")]
    internal List<DsExecutionContext> Executions { get; private set; }

    public override int ChildCount() => throw new NotImplementedException();

    LiteDbDataSource _ds;

    public override IReadOnlyList<BfExecutionContext> GetExecutions() => Executions;

    public override BfOrderContextBase GetChild(int childIndex)
    {
        return Children[childIndex];
    }

    public override void SetChild(int childIndex, BfOrderContextBase child)
    {
        Children[childIndex] = (DbOrderContext)child;
    }

    protected override void SetChildrenSize(int count)
    {
        throw new NotImplementedException();
    }

    public DbOrderContext(LiteDbDataSource ds, string productCode)
        : base(productCode)
    {
        _ds = ds;
    }

    public DbOrderContext(LiteDbDataSource ds, string productCode, BfOrderType orderType)
        : base(productCode, orderType)
    {
        _ds = ds;
    }

    public override BfOrderContextBase ContextUpdated()
    {
        return _ds.Upsert(this);
    }
}
