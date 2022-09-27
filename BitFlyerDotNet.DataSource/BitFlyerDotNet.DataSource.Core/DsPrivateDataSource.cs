//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.DataSource;

public class DsPrivateDataSource : BfPrivateDataSource
{
    LiteDatabase _db;
    ILiteCollection<DsOrderContext> _colOrders;

    public DsPrivateDataSource(IBfApplication app) : base(app)
    {
        _db = new LiteDatabase(app.GetConfig().GetValue<string>("CacheFilePath")) { UtcDate = true };
        _colOrders = _db.GetCollection<DsOrderContext>("orders");
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    public override BfOrderContext CreateOrderContext(string productCode)
    {
        return new DsOrderContext(this, productCode);
    }

    public override BfOrderContext GetOrCreateOrderContext(string productCode, string acceptanceId)
    {
        return TryGetOnCache(productCode, acceptanceId, out var ctx) ? ctx : new DsOrderContext(this, productCode);
    }

    public override bool TryGetOnCache(string productCode, string orderId, out BfOrderContext ctx)
    {
        var dbctx = _colOrders.Query().Where($"ANY({nameof(DsOrderContext.Children)}[@.{nameof(DsOrderContext.OrderId)} = '{orderId}']) = true");
        if (dbctx.Count() == 0)
        {
            ctx = default;
            return false;
        }

        ctx = dbctx.First();
        return true;
    }

    public override IAsyncEnumerable<BfOrderContext> GetRecentOrderContextsAsync(string productCode, TimeSpan span)
    {
        throw new NotSupportedException();
    }
}
