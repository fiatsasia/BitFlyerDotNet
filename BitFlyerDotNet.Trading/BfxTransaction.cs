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
    public BfOrderContextBase GetOrderContext() => _ctx;
    public event EventHandler<BfxOrderChangedEventArgs>? OrderChanged;

    BfxApplication _app;
    BfOrderContextBase _ctx;
    CancellationTokenSource _ctsCancelOrder;

    public BfxTransaction(BfxApplication app, BfOrderContextBase ctx)
    {
        _app = app;
        _ctx = ctx;
        _ctsCancelOrder = new CancellationTokenSource();
    }

    public void Dispose()
    {
        _ctsCancelOrder.Cancel();
        _ctsCancelOrder.Dispose();
    }

    public async Task<string> PlaceOrderAsync<TOrder>(TOrder order, CancellationToken ct) where TOrder : IBfOrder
    {
        using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, _ctsCancelOrder.Token))
        {
            cts.CancelAfter(_app.Config.SendOrderTimeout);
            try
            {
                for (var retry = 0; retry <= _app.Config.OrderRetryMax; retry++)
                {
                    BitFlyerResponse resp;
                    string id = string.Empty;
                    if (order is BfChildOrder childOrder)
                    {
                        resp = await _app.Client.SendChildOrderAsync(childOrder, cts.Token);
                        if (!resp.IsError)
                        {
                            id = resp.Deserialize<BfChildOrderAcceptance>().ChildOrderAcceptanceId;
                        }
                    }
                    else if (order is BfParentOrder parentOrder)
                    {
                        resp = await _app.Client.SendParentOrderAsync(parentOrder, cts.Token);
                        if (!resp.IsError)
                        {
                            id = resp.Deserialize<BfParentOrderAcceptance>().ParentOrderAcceptanceId;
                        }
                    }
                    else
                    {
                        throw new ArgumentException();
                    }

                    if (!string.IsNullOrEmpty(id))
                    {
                        _ctx.OrderAccepted(id).ContextUpdated();
                        OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.OrderAccepted, _ctx));
                        return id;
                    }

                    Log.Warn($"SendChildOrder failed: {resp.StatusCode} {resp.ErrorMessage}");
                    Log.Info("Trying retry...");
                    OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.RetryingOrder, _ctx));
                    await Task.Delay(_app.Config.OrderRetryInterval);
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
            cts.CancelAfter(_app.Config.SendCancelOrderTimeout);
            if (!_ctx.IsActive)
            {
                Log.Debug("Cancel order requested but not active");
                OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.CancelIgnored, _ctx));
                return;
            }

            BitFlyerResponse resp;
            if (!_ctx.HasChildren)
            {
                resp = await _app.Client.CancelChildOrderAsync(_ctx.ProductCode, string.Empty, _ctx.OrderAcceptanceId, cts?.Token ?? CancellationToken.None);
            }
            else
            {
                resp = await _app.Client.CancelParentOrderAsync(_ctx.ProductCode, string.Empty, _ctx.OrderAcceptanceId, cts?.Token ?? CancellationToken.None);
            }
            if (!resp.IsError)
            {
                OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.CancelAccepted, _ctx));
            }
            else
            {
                OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.CancelRejected, _ctx));
            }
        }
    }

    public BfxTransaction OnOrderEvent(IBfOrderEvent e)
    {
        OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(e, _ctx));
        return this;
    }
}
