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

    public virtual bool FindParent(string childOrderAcceptanceId, out BfxOrderContext parent)
    {
        foreach (var ctx in _ctx.Values.ToArray())
        {
            if (!ctx.HasChildren)
            {
                continue;
            }
            var childIndex = ctx.Children.ToList().FindIndex(e => e.OrderAcceptanceId == childOrderAcceptanceId);
            if (childIndex >= 0)
            {
                parent = ctx;
                return true;
            }
        }
        parent = null;
        return false;
    }

    public virtual IAsyncEnumerable<BfxOrderContext> GetOrderContextsAsync(string productCode)
        => _ctx.Values.ToList().Where(e => e.ProductCode == productCode).ToAsyncEnumerable();

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
}
