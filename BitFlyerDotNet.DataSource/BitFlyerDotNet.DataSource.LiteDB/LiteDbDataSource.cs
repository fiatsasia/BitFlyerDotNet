//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.DataSource;

public class LiteDbDataSource : BdPrivateDataSource
{
    LiteDatabase _db;
    ILiteCollection<DbOrderContext> _colOrders;

    public LiteDbDataSource(IBfApplication app) : base(app)
    {
        _db = new LiteDatabase(app.GetConfig().GetValue<string>("CacheFilePath")) { UtcDate = true };
        _colOrders = _db.GetCollection<DbOrderContext>("orders");
    }

    public LiteDbDataSource(BitFlyerClient client, string filePath)
        : base(client)
    {
        _db = new LiteDatabase(filePath) { UtcDate = true };
        _colOrders = _db.GetCollection<DbOrderContext>("orders");
    }

    public override void Dispose()
    {
        _db.Dispose();
    }

    public override BdOrderContext CreateOrderContext(string productCode)
    {
        return new DbOrderContext(this, productCode);
    }

    public override BdOrderContext GetOrCreateOrderContext(string productCode, string acceptanceId)
    {
        return TryGetOnCache(productCode, acceptanceId, out var ctx) ? ctx : new DbOrderContext(this, productCode);
    }

    public override bool TryGetOnCache(string productCode, string orderId, out BdOrderContext ctx)
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

    public override IAsyncEnumerable<BdOrderContext> GetRecentOrderContextsAsync(string productCode, TimeSpan span)
    {
        throw new NotSupportedException();
    }
}
