//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System.Runtime.CompilerServices;

namespace BitFlyerDotNet.LightningApi;

public class BfPrivateDataSource
{
    IBfApplication _app;
    ConcurrentDictionary<string, ConcurrentDictionary<string, BfOrderContext>> _ctxs = new();
    BfPositionManager _positions;

    public BfPrivateDataSource(IBfApplication app)
    {
        _app = app;
    }

    public virtual BfOrderContext CreateOrderContext(string productCode)
    {
        return new BfOrderContext(this, productCode);
    }

    public virtual BfOrderContext GetOrCreateOrderContext(string productCode, string acceptanceId)
    {
        return TryGetOnCache(productCode, acceptanceId, out var ctx) ? ctx : new BfOrderContext(this, productCode);
    }

    public BfOrderContext Upsert(BfOrderContext ctx)
    {
        _ctxs.GetOrAdd(ctx.ProductCode, _ => new()).TryAdd(ctx.OrderAcceptanceId, ctx);
        return ctx;
    }

    public virtual bool TryGetOnCache(string productCode, string orderId, out BfOrderContext ctx)
        => _ctxs.GetOrAdd(productCode, _ => new()).TryGetValue(orderId, out ctx);

    async IAsyncEnumerable<BfOrderContext> GetOrderContextsAsync(string productCode, BfOrderState state, Func<BfParentOrderStatus, bool> parentPredicate, Func<BfChildOrderStatus, bool> childPredicate, [EnumeratorCancellation] CancellationToken ct)
    {
        var execExpireDate = DateTime.UtcNow - TimeSpan.FromDays(30);
        await foreach (var parentOrder in _app.Client.GetParentOrdersAsync(productCode, state, 0, 0, 0, parentPredicate, ct))
        {
            var parentOrderDetail = await _app.Client.GetParentOrderAsync(productCode, parentOrderId: parentOrder.ParentOrderId);
            var ctx = GetOrCreateOrderContext(productCode, parentOrder.ParentOrderAcceptanceId).Update(parentOrder, parentOrderDetail);
            foreach (var childOrder in await _app.Client.GetChildOrdersAsync(productCode, parentOrderId: parentOrder.ParentOrderId))
            {
                var execs = default(BfPrivateExecution[]);
                if (childOrder.ExecutedSize > 0m && childOrder.ExpireDate > execExpireDate)
                {
                    execs = await _app.Client.GetPrivateExecutionsAsync(productCode, childOrderId: childOrder.ChildOrderId);
                }
                ctx.UpdateChild(childOrder, execs);
            }
            ctx.ContextUpdated();
            yield return ctx;
        }

        await foreach (var order in _app.Client.GetChildOrdersAsync(productCode, state, 0, 0, 0, "", "", "", childPredicate, ct))
        {
            if (TryGetOnCache(productCode, order.ChildOrderId, out var ctx))
            {
                ctx.Update(order).ContextUpdated();
                yield return ctx;
            }

            var execs = default(BfPrivateExecution[]);
            if (order.ExecutedSize > 0m && order.ExpireDate > execExpireDate)
            {
                execs = await _app.Client.GetPrivateExecutionsAsync(productCode, childOrderId: order.ChildOrderId);
            }
            yield return CreateOrderContext(productCode).Update(order, execs).ContextUpdated();
        }
    }

    public virtual IAsyncEnumerable<BfOrderContext> GetActiveOrderContextsAsync(string productCode)
        => GetOrderContextsAsync(productCode, BfOrderState.Active, e => true, e => true, CancellationToken.None);

    public virtual IAsyncEnumerable<BfOrderContext> GetRecentOrderContextsAsync(string productCode, TimeSpan span)
    {
        var now = DateTime.UtcNow;
        return GetOrderContextsAsync(productCode, BfOrderState.All, e => (now - e.ParentOrderDate) <= span, e => (now - e.ChildOrderDate) <= span, CancellationToken.None);
    }

    public async Task InitializePositionsAsync(string productCode)
    {
        if (_positions == default)
        {
            _positions = new(await _app.Client.GetPositionsAsync(productCode));
        }
    }

    public async IAsyncEnumerable<BfxPosition> GetActivePositionsAsync(string productCode)
    {
        if (_positions == default)
        {
            await InitializePositionsAsync(productCode);
        }
        foreach (var pos in _positions.GetActivePositions()) yield return pos;
    }

    public async IAsyncEnumerable<BfxPosition> UpdatePositionAsync(BfChildOrderEvent e)
    {
        await foreach (var pos in _positions.Update(e).ToAsyncEnumerable()) yield return pos;
    }

    public Task<decimal> GetTotalPositionSizeAsync() => Task.FromResult(_positions.TotalSize);
}
