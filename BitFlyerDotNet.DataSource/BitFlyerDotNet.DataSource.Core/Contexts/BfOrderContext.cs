//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.DataSource;

public class BfOrderContext : BfOrderContextBase
{
    [JsonProperty]
    internal List<BfOrderContext> Children { get; private set; }
    public override IEnumerable<BfOrderContextBase> GetChildren() => Children.Cast<BfOrderContextBase>();
    public override BfOrderContextBase GetChild(int childIndex) => Children != default ? Children[childIndex] : default;
    public override void SetChild(int childIndex, BfOrderContextBase child) => Children[childIndex] = (BfOrderContext)child;
    public override int ChildCount() => Children != default ? Children.Count : 0;

    BfPrivateDataSource _ds;

    public BfOrderContext(BfPrivateDataSource ds, string productCode)
        : base(productCode)
    {
        _ds = ds;
    }

    public BfOrderContext(BfPrivateDataSource ds, string productCode, BfOrderType orderType)
        : base(productCode, orderType)
    {
        _ds = ds;
    }

    protected override void SetChildrenSize(int count)
    {
        if (Children == default)
        {
            Children = new();
        }
        else if (ChildCount() >= count)
        {
            return;
        }

        while (ChildCount() < count)
        {
            var child = new BfOrderContext(_ds, ProductCode);
            child.SetParent(this);
            Children.Add(child);
        }
    }

    public override BfOrderContextBase ContextUpdated()
    {
        return _ds.Upsert(this);
    }
}
