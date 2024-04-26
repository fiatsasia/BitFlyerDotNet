//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System.Reflection;

namespace BitFlyerDotNet.DataSource;

public class LiteDbDataSource : BfPrivateDataSource
{
    LiteDatabase _db;
    ILiteCollection<DbOrderIndexContext> _colIndex;
    ILiteCollection<DbOrderContext> _colOrders;

    public LiteDbDataSource(IBfApplication app) : base(app)
    {
        _db = new LiteDatabase(app.GetConfig().GetValue<string>("CacheFilePath")) { UtcDate = true };
        _colIndex = _db.GetCollection<DbOrderIndexContext>("index");
        _colOrders = _db.GetCollection<DbOrderContext>("orders");
    }

    public LiteDbDataSource(BitFlyerClient client, string filePath)
        : base(client)
    {
        _db = new LiteDatabase(filePath) { UtcDate = true };
        _colIndex = _db.GetCollection<DbOrderIndexContext>("index");
        _colOrders = _db.GetCollection<DbOrderContext>("orders");
    }

    public LiteDbDataSource(BitFlyerClient client)
        : base(client)
    {
        var dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BitFlyerDotNet");
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        var filePath = Path.Combine(dirPath, $"{Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location)}.db");
        _db = new LiteDatabase(filePath) { UtcDate = true };
        _colIndex = _db.GetCollection<DbOrderIndexContext>("index");
        _colOrders = _db.GetCollection<DbOrderContext>("orders");
    }

    public override void Dispose()
    {
        _db.Dispose();
    }

    public override BfOrderContextBase CreateOrderContext(string productCode)
    {
        return new DbOrderContext(this, productCode);
    }

    public override BfOrderContextBase GetOrCreateOrderContext(string productCode, string acceptanceId)
    {
        return TryGetOnCache(productCode, acceptanceId, out var ctx) ? ctx : new DbOrderContext(this, productCode);
    }

    public override bool TryGetOnCache(string productCode, string orderId, out BfOrderContextBase ctx)
    {
        var dbctx = _colOrders.Query().Where($"ANY({nameof(DbOrderContext.Children)}[@.{nameof(DbOrderContext.OrderId)} = '{orderId}']) = true");
        if (dbctx.Count() == 0)
        {
            ctx = default;
            return false;
        }

        ctx = dbctx.First();
        return true;
    }

    public override async IAsyncEnumerable<BfOrderContextBase> GetRecentOrderContextsAsync(string productCode, TimeSpan span)
    {
        var index = _colIndex.FindOne(e => e.ProductCode == productCode);
        if (index == null)
        {
            index = new();
            index.ProductCode = productCode;
            index.ParentOrderIdMin = index.ChildOrderIdMin = long.MaxValue;
            index.ParentOrderIdMax = index.ChildOrderIdMax = 0L;
            index.ParentOrderCount = index.ChildOrderCount = 0;
            _colIndex.Insert(index);
        }

        var now = DateTime.UtcNow;
        var execExpireDate = DateTime.UtcNow - TimeSpan.FromDays(30);
        await foreach (var parentOrder in Client.GetParentOrdersAsync(productCode, BfOrderState.All, 0, 0, index.ParentOrderIdMax, e => (now - e.ParentOrderDate) <= span, CancellationToken.None))
        {
            index.Update(parentOrder);
            var parentOrderDetail = await Client.GetParentOrderAsync(productCode, parentOrderId: parentOrder.ParentOrderId);
            var ctx = GetOrCreateOrderContext(productCode, parentOrder.ParentOrderAcceptanceId).Update(parentOrder, parentOrderDetail);
            foreach (var childOrder in await Client.GetChildOrdersAsync(productCode, parentOrderId: parentOrder.ParentOrderId))
            {
                var execs = default(BfPrivateExecution[]);
                if (childOrder.ExecutedSize > 0m && childOrder.ExpireDate > execExpireDate)
                {
                    execs = await Client.GetPrivateExecutionsAsync(productCode, childOrderId: childOrder.ChildOrderId);
                }
                //
                // 元のOrderTupeが上書きされて本体が判らなくなる StopLimit -> Limit
                //
                ctx.UpdateChild(CreateOrderContext(productCode).Update(childOrder, execs).ContextUpdated());
            }
            ctx.ContextUpdated();
            yield return ctx;
        }

        throw new NotSupportedException();
    }
}
