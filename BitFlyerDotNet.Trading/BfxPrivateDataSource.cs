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
            if (ctx.Children.Count == 0)
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

    public async virtual IAsyncEnumerable<BfxOrderContext> GetActiveOrders(string productCode)
    {
        var childOrders = await _client.GetChildOrdersAsync(productCode, orderState: BfOrderState.Active);
        foreach (var order in childOrders)
        {
            var execs = await _client.GetPrivateExecutionsAsync(productCode, childOrderId: order.ChildOrderId);
            yield return _ctx.AddOrUpdate(order.ChildOrderAcceptanceId,
                _ => new BfxOrderContext(productCode).Update(order, execs),
                (_, ctx) => ctx.Update(order, execs)
            );
        }

        await foreach (var parentOrder in _client.GetParentOrdersAsync(productCode, BfOrderState.Active, 0, 0, e => true))
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
                    _ => new BfxOrderContext(productCode).Update(childOrder, execs),
                    (_, ctx) => ctx.Update(childOrder, execs)
                );
            }
            yield return pctx;
        }
    }

    private async IAsyncEnumerable<BfxOrderContext> GetOrdersAsync(string productCode, BfOrderState orderState, int count, bool linkChildToParent, Func<BfxOrderContext, bool> predicate)
    {
#pragma warning disable CS8604
        var ctxs = new Dictionary<string, BfxOrderContext>();

        // Get child orders
        await foreach (var childOrder in _client.GetChildOrdersAsync(productCode, orderState, count, 0, e => true))
        {
            var ctx = new BfxOrderContext(productCode);
            var execs = await _client.GetPrivateExecutionsAsync(productCode, childOrderId: childOrder.ChildOrderId);
            ctx.Update(childOrder, execs);
            if (!predicate(ctx))
            {
                break;
            }
            ctxs.Add(ctx.OrderAcceptanceId, ctx);
        }

        // Get parent orders
        await foreach (var parentOrder in _client.GetParentOrdersAsync(productCode, orderState, count, 0, e => true))
        {
            var ctx = new BfxOrderContext(productCode);
            var parentOrderDetail = await _client.GetParentOrderAsync(productCode, parentOrderId: parentOrder.ParentOrderId);
            ctx.Update(parentOrder, parentOrderDetail);
            if (!predicate(ctx))
            {
                break;
            }
            ctxs.Add(ctx.OrderAcceptanceId, ctx);
        }

        // Link child to parent
        if (linkChildToParent)
        {
            foreach (var parentOrder in ctxs.Values.Where(e => (e.OrderType != BfOrderType.Market && e.OrderType != BfOrderType.Limit)))
            {
                foreach (var childOrder in await _client.GetChildOrdersAsync(productCode, parentOrderId: parentOrder.OrderId))
                {
                    var execs = await _client.GetPrivateExecutionsAsync(productCode, childOrderId: childOrder.ChildOrderId);
                    ctxs[parentOrder.OrderAcceptanceId].UpdateChild(childOrder, execs);
                    ctxs.Remove(childOrder.ChildOrderAcceptanceId);
                }
            }
        }

        foreach (var trade in ctxs.Values)
        {
            yield return trade;
        }
#pragma warning restore CS8604
    }

    public async IAsyncEnumerable<BfxOrder> GetRecentOrdersAsync(string productCode, int count)
    {
        if (!_client.IsAuthenticated)
        {
            throw new BitFlyerUnauthorizedException();
        }

        await foreach (var trade in GetOrdersAsync(productCode, BfOrderState.Unknown, count, true, e => true))
        {
            yield return new BfxOrder(trade);
        }
    }
}
