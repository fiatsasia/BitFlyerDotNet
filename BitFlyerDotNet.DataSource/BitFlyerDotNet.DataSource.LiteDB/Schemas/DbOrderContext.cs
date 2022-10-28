using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace BitFlyerDotNet.DataSource;

internal class DbExecutionContext : BfExecutionContext
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
    internal List<DbExecutionContext> Executions { get; private set; }

    public override int ChildCount() => throw new NotImplementedException();

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


    LiteDbDataSource _ds;

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

internal class DbOrderIndexContext
{
    [BsonId]
    public LiteDB.ObjectId Oid { get; set; }

    public string ProductCode { get; set; }

    public long ParentOrderIdMin { get; set; }

    public long ParentOrderIdMax { get; set; }
    public long ParentOrderCount { get; set; }

    public long ChildOrderIdMin { get; set; }

    public long ChildOrderIdMax { get; set; }
    public long ChildOrderCount { get; set; }

    public void Update(BfParentOrderStatus order)
    {
        ParentOrderIdMax = Math.Max(ParentOrderIdMax, order.Id);
        ParentOrderIdMin = Math.Min(ParentOrderIdMin, order.Id);
    }
}