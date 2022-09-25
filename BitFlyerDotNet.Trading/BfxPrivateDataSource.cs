//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

class BfxPrivateDataSource
{
    BitFlyerClient _client;
    BfxConfiguration _config;
    ConcurrentDictionary<string, ConcurrentDictionary<string, BfOrderContext>> _ctxs = new();
    BfxPositionManager _positions;

    public BfxPrivateDataSource(BitFlyerClient client, BfxConfiguration config)
    {
        _client = client;
        _config = config;
    }

    public BfOrderContext CreateOrderContext(string productCode)
    {
        return new BfOrderContext(productCode);
    }

    public BfOrderContext GetOrCreateOrderContext(string productCode, string acceptanceId)
    {
        return _ctxs.GetOrAdd(productCode, _ => new()).TryGetValue(acceptanceId, out var ctx) ? ctx : new BfOrderContext(productCode);
    }

    public BfOrderContext TryRegisterOrderContext(string productCode, string acceptanceId, BfOrderContext ctx)
    {
        _ctxs.GetOrAdd(productCode, _ => new()).TryAdd(acceptanceId, ctx);
        return ctx;
    }

    public async IAsyncEnumerable<BfOrderContext> GetOrderServerContextsAsync(string productCode, BfOrderState orderState, int count)
    {
        var ctx = _ctxs.GetOrAdd(productCode, _ => new());

        // Get child active orders
        await foreach (var order in _client.GetChildOrdersAsync<BfChildOrderStatus>(productCode, orderState, count, 0, 0, "", "", "", e => true, CancellationToken.None))
        {
            var execs = await _client.GetPrivateExecutionsAsync(productCode, childOrderId: order.ChildOrderId);
            yield return ctx.AddOrUpdate(order.ChildOrderAcceptanceId,
                _ => new BfOrderContext(productCode).Update(order, execs),
                (_, ctx) => ctx.Update(order, execs)
            );
        }

        // Get parent orders
        await foreach (var parentOrder in _client.GetParentOrdersAsync(productCode, orderState, count, 0, 0, e => true, CancellationToken.None))
        {
            var parentOrderDetail = await _client.GetParentOrderAsync(productCode, parentOrderId: parentOrder.ParentOrderId);
            var pctx = ctx.AddOrUpdate(parentOrder.ParentOrderAcceptanceId,
                _ => new BfOrderContext(productCode).Update(parentOrder, parentOrderDetail),
                (_, ctx) => ctx.Update(parentOrder, parentOrderDetail)
            );
            foreach (var childOrder in await _client.GetChildOrdersAsync(productCode, parentOrderId: parentOrder.ParentOrderId))
            {
                var execs = await _client.GetPrivateExecutionsAsync(productCode, childOrderId: childOrder.ChildOrderId);
                pctx.UpdateChild(childOrder, execs);
                ctx.AddOrUpdate(childOrder.ChildOrderAcceptanceId,
                    _ => new BfOrderContext(productCode).Update(childOrder, execs),
                    (_, ctx) => ctx.Update(childOrder, execs)
                );
            }
            yield return pctx;
        }
    }

    public IEnumerable<BfOrderContext> GetOrderCacheContexts(string productCode)
        => _ctxs.GetOrAdd(productCode, _ => new()).Values.ToList();

    public async Task InitializePositionsAsync(string productCode)
    {
        if (_positions == default)
        {
            _positions = new(await _client.GetPositionsAsync(productCode));
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
