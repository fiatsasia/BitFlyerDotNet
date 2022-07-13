//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

class BfxTransaction
{
    public Ulid Id { get; } = Ulid.NewUlid();
    public BfxOrderContext GetOrderContext() => _ctx;
    public event EventHandler<BfxOrderChangedEventArgs>? OrderChanged;

    BitFlyerClient _client;
    CancellationTokenSource _ctsOrder;
    BfxOrderContext _ctx;
    BfxConfiguration _config;

    internal BfxTransaction(BitFlyerClient client, BfxOrderContext ctx, BfxConfiguration config)
    {
        _client = client;
        _config = config;
        _ctx = ctx;
    }

    #region Child order
    public async Task<string> PlaceOrderAsync(BfChildOrder order, CancellationTokenSource cts)
    {
        _ctsOrder = (cts != default) ? cts : new CancellationTokenSource();
        for (var retry = 0; retry <= _config.OrderRetryMax; retry++)
        {
            if (_ctsOrder.IsCancellationRequested)
            {
                Log.Debug("SendChildOrder - canceled");
                return string.Empty;
            }
            var resp = await _client.SendChildOrderAsync(order, _ctsOrder.Token);
            if (!resp.IsError)
            {
                var id = resp.GetContent().ChildOrderAcceptanceId;
                _ctx.OrderAccepted(id);
                OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.OrderAccepted, _ctx));
                return id;
            }

            Log.Warn($"SendChildOrder failed: {resp.StatusCode} {resp.ErrorMessage}");
            if (_ctsOrder.IsCancellationRequested)
            {
                Log.Debug("SendChildOrder - canceled");
                return string.Empty;
            }
            Log.Info("Trying retry...");
            OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.RetryingOrder, _ctx));
            await Task.Delay(_config.OrderRetryInterval);
        }

        Log.Error("SendChildOrder - Retried out");
        OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.RetriedOut, _ctx));
        return string.Empty;
    }

    public async Task CancelChildOrderAsync(CancellationTokenSource cts)
    {
        _ctsOrder.Cancel();
        if (!_ctx.IsActive)
        {
            Log.Debug("Cancel order requested but not active");
            OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.CancelIgnored, _ctx));
            return;
        }

        var resp = await _client.CancelChildOrderAsync(_ctx.ProductCode, string.Empty, _ctx.OrderAcceptanceId, cts?.Token ?? CancellationToken.None);
        if (resp.IsError)
        {
            OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.CancelAccepted, _ctx));
        }
        else
        {
            OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.CancelRejected, _ctx));
        }
    }

    internal BfxTransaction OnChildOrderEvent(BfChildOrderEvent e)
    {
        OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(e, _ctx));
        return this;
    }
    #endregion Child order

    #region Parent order
    // - 経過時間でリトライ終了のオプション
    public async Task<string> PlaceOrdertAsync(BfParentOrder order, CancellationTokenSource cts)
    {
        _ctsOrder = (cts != default) ? cts : new CancellationTokenSource();
        for (var retry = 0; retry <= _config.OrderRetryMax; retry++)
        {
            if (_ctsOrder.IsCancellationRequested)
            {
                Log.Debug("SendParentOrder - canceled");
                return string.Empty;
            }
            var resp = await _client.SendParentOrderAsync(order, _ctsOrder.Token);
            if (!resp.IsError)
            {
                var id = resp.GetContent().ParentOrderAcceptanceId;
                _ctx.OrderAccepted(id);
                OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.OrderAccepted, _ctx));
                return id;
            }

            Log.Warn($"SendParentOrder failed: {resp.StatusCode} {resp.ErrorMessage}");
            if (_ctsOrder.IsCancellationRequested)
            {
                Log.Debug("SendParentOrder - canceled");
                return string.Empty;
            }
            Log.Info("Trying retry...");
            OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.RetryingOrder, _ctx));
            await Task.Delay(_config.OrderRetryInterval);
        }

        Log.Error("SendParentOrder - Retried out");
        OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.RetriedOut, _ctx));
        return string.Empty;
    }

    public async Task CancelParentOrderAsync(CancellationTokenSource cts)
    {
        _ctsOrder.Cancel();
        if (!_ctx.IsActive)
        {
            Log.Debug("Cancel order requested but not active");
            OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.CancelIgnored, _ctx));
            return;
        }

        var resp = await _client.CancelParentOrderAsync(_ctx.ProductCode, string.Empty, _ctx.OrderAcceptanceId, cts?.Token ?? CancellationToken.None);
        if (resp.IsError)
        {
            OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.CancelAccepted, _ctx));
        }
        else
        {
            OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.CancelRejected, _ctx));
        }
    }

    internal BfxTransaction OnParentOrderEvent(BfParentOrderEvent e)
    {
        OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(e, _ctx));
        return this;
    }
    #endregion Parent order
}
