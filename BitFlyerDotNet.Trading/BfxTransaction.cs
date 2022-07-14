//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

class BfxTransaction : IDisposable
{
    public Ulid Id { get; } = Ulid.NewUlid();
    public BfxOrderContext GetOrderContext() => _ctx;
    public event EventHandler<BfxOrderChangedEventArgs>? OrderChanged;

    BitFlyerClient _client;
    BfxOrderContext _ctx;
    BfxConfiguration _config;
    CancellationTokenSource _ctsCancelOrder;

    public BfxTransaction(BitFlyerClient client, BfxOrderContext ctx, BfxConfiguration config)
    {
        _client = client;
        _config = config;
        _ctx = ctx;
        _ctsCancelOrder = new CancellationTokenSource();
    }

    public void Dispose()
    {
        _ctsCancelOrder.Cancel();
        _ctsCancelOrder.Dispose();
    }

    public async Task<string> PlaceOrderAsync<TOrder>(TOrder order, CancellationToken ct)
    {
        using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, _ctsCancelOrder.Token))
        {
            cts.CancelAfter(_config.SendOrderTimeout);
            try
            {
                for (var retry = 0; retry <= _config.OrderRetryMax; retry++)
                {
                    BitFlyerResponse resp;
                    string id = string.Empty;
                    if (order.GetType() == typeof(BfChildOrder))
                    {
                        resp = await _client.SendChildOrderAsync(order as BfChildOrder, cts.Token);
                        if (!resp.IsError)
                        {
                            id = resp.GetContent<BfChildOrderResponse>().ChildOrderAcceptanceId;
                        }
                    }
                    else if (order.GetType() == typeof(BfParentOrder))
                    {
                        resp = await _client.SendParentOrderAsync(order as BfParentOrder, cts.Token);
                        if (!resp.IsError)
                        {
                            id = resp.GetContent<BfParentOrderResponse>().ParentOrderAcceptanceId;
                        }
                    }
                    else
                    {
                        throw new ArgumentException();
                    }

                    if (!string.IsNullOrEmpty(id))
                    {
                        _ctx.OrderAccepted(id);
                        OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.OrderAccepted, _ctx));
                        return id;
                    }

                    Log.Warn($"SendChildOrder failed: {resp.StatusCode} {resp.ErrorMessage}");
                    Log.Info("Trying retry...");
                    OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.RetryingOrder, _ctx));
                    await Task.Delay(_config.OrderRetryInterval);
                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cts.Token)
            {
                if (ct.IsCancellationRequested)
                {
                    Log.Debug("PlaceChildOrder - canceled by user");
                    throw new OperationCanceledException(ex.Message, ex, ct);
                }
                else if (_ctsCancelOrder.IsCancellationRequested)
                {
                    Log.Debug("PlaceChildOrder - canceled by CancelOrder()");
                    throw new OperationCanceledException("PlaceOrder canceled by CancelOrder request");
                }
                else
                {
                    Log.Debug("PlaceChildOrder - Timedout due to in configuration");
                    throw new TimeoutException("Timedout due to in configuraiton", ex);
                }
            }

            Log.Error("SendChildOrder - Retried out");
            OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.RetriedOut, _ctx));
            return string.Empty;
        }
    }

    public async Task CancelOrderAsync(CancellationToken ct)
    {
        _ctsCancelOrder.Cancel();
        using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            cts.CancelAfter(_config.SendCancelOrderTimeout);
            if (!_ctx.IsActive)
            {
                Log.Debug("Cancel order requested but not active");
                OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.CancelIgnored, _ctx));
                return;
            }

            BitFlyerResponse resp;
            if (_ctx.HasChildren)
            {
                resp = await _client.CancelChildOrderAsync(_ctx.ProductCode, string.Empty, _ctx.OrderAcceptanceId, cts?.Token ?? CancellationToken.None);
            }
            else
            {
                resp = await _client.CancelParentOrderAsync(_ctx.ProductCode, string.Empty, _ctx.OrderAcceptanceId, cts?.Token ?? CancellationToken.None);
            }
            if (resp.IsError)
            {
                OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.CancelAccepted, _ctx));
            }
            else
            {
                OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.CancelRejected, _ctx));
            }
        }
    }

    internal BfxTransaction OnChildOrderEvent(BfChildOrderEvent e)
    {
        OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(e, _ctx));
        return this;
    }

    internal BfxTransaction OnParentOrderEvent(BfParentOrderEvent e)
    {
        OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(e, _ctx));
        return this;
    }
}
