//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

public class BfxPrivateDataSource
{
    BitFlyerClient _client;
    ConcurrentDictionary<string, BfxOrderContext> _ctx = new();
    BfxPositionManager _positions;

    public BfxPrivateDataSource(BitFlyerClient client)
    {
        _client = client;
    }

    public virtual BfxOrderContext CreateOrderContext(string productCode)
    {
        return new BfxOrderContext(productCode);
    }

    public virtual BfxOrderContext GetOrCreateOrderContext(string productCode, string acceptanceId)
    {
        return _ctx.TryGetValue(acceptanceId, out var ctx) ? ctx : new BfxOrderContext(productCode);
    }

    public virtual BfxOrderContext TryRegisterOrderContext(string acceptanceId, BfxOrderContext ctx)
    {
        _ctx.TryAdd(acceptanceId, ctx);
        return ctx;
    }

    public virtual bool FindParent(string childOrderAcceptanceId, out BfxOrderContext parent)
    {
        parent = default;
        return (_ctx.TryGetValue(childOrderAcceptanceId, out var child) &&
            !string.IsNullOrEmpty(child.ParentOrderAcceptanceId) &&
            _ctx.TryGetValue(child.ParentOrderAcceptanceId, out parent)
        );
    }

    public virtual IAsyncEnumerable<BfxOrderContext> GetOrderContextsAsync(string productCode)
    {
        // Remove child order which belonged parent order
        var orders = _ctx.Values.Where(e => e.ProductCode == productCode).ToDictionary(e => e.OrderAcceptanceId, e => e);
        foreach (var parentOrder in orders.Values.Where(e => e.HasChildren).ToList())
        {
            foreach (var childOrder in parentOrder.Children)
            {
                if (!string.IsNullOrEmpty(childOrder.OrderAcceptanceId))
                {
                    orders.Remove(childOrder.OrderAcceptanceId);
                }
            }
        }
        return orders.Values.ToList().ToAsyncEnumerable();
    }

    public async IAsyncEnumerable<BfxOrderContext> GetOrderContextsAsync(string productCode, BfOrderState orderState, int count)
    {
        // Get child active orders
        await foreach (var order in _client.GetChildOrdersAsync(productCode, orderState, count, 0, e => true))
        {
            var execs = await _client.GetPrivateExecutionsAsync(productCode, childOrderId: order.ChildOrderId);
            yield return _ctx.AddOrUpdate(order.ChildOrderAcceptanceId,
                _ => new BfxOrderContext(productCode).Update(order, execs),
                (_, ctx) => ctx.Update(order, execs)
            );
        }

        // Get parent orders
        await foreach (var parentOrder in _client.GetParentOrdersAsync(productCode, orderState, count, 0, e => true))
        {
            var parentOrderDetail = await _client.GetParentOrderAsync(productCode, parentOrderId: parentOrder.ParentOrderId);
            var pctx = _ctx.AddOrUpdate(parentOrder.ParentOrderAcceptanceId,
                _ => new BfxOrderContext(productCode).Update(parentOrder, parentOrderDetail),
                (_, ctx) => ctx.Update(parentOrder, parentOrderDetail)
            );
            foreach (var childOrder in await _client.GetChildOrdersAsync(productCode, parentOrderId: parentOrder.ParentOrderId))
            {
                var execs = await _client.GetPrivateExecutionsAsync(productCode, childOrderId: childOrder.ChildOrderId);
                pctx.UpdateChild(childOrder, execs);
                _ctx.AddOrUpdate(childOrder.ChildOrderAcceptanceId,
                    _ => new BfxOrderContext(productCode).Update(childOrder, execs).SetParent(pctx.OrderAcceptanceId),
                    (_, ctx) => ctx.Update(childOrder, execs).SetParent(pctx.OrderAcceptanceId)
                );
            }
            yield return pctx;
        }
    }

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
