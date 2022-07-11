//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

public class BfxTransaction
{
    public BfxOrderContext GetOrderContext() => _ctx;
    public event EventHandler<BfxOrderChangedEventArgs>? OrderChanged;

    BitFlyerClient _client;
    CancellationTokenSource _cts = new CancellationTokenSource();
    BfxOrderContext _ctx;
    BfxConfiguration _config;

    internal BfxTransaction(BitFlyerClient client, BfxOrderContext ctx, BfxConfiguration config)
    {
        _client = client;
        _config = config;
        _ctx = ctx;
    }

    #region Child order
    public async Task<string> PlaceOrderAsync(BfChildOrder order)
    {
        for (var retry = 0; retry <= _config.OrderRetryMax; retry++)
        {
            if (_cts.IsCancellationRequested)
            {
                Log.Debug("SendChildOrder - canceled");
                return string.Empty;
            }
            var resp = await _client.SendChildOrderAsync(order, _cts.Token);
            if (!resp.IsError)
            {
                var id = resp.GetContent().ChildOrderAcceptanceId;
                _ctx.OrderAccepted(id);
                OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.OrderAccepted, _ctx));
                return id;
            }

            Log.Warn($"SendChildOrder failed: {resp.StatusCode} {resp.ErrorMessage}");
            if (_cts.IsCancellationRequested)
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

    internal BfxTransaction OnChildOrderEvent(BfChildOrderEvent e)
    {
        OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(e, _ctx));
        return this;
    }
    #endregion Child order

    #region Parent order
    // - 経過時間でリトライ終了のオプション
    public async Task<string> PlaceOrdertAsync(BfParentOrder order)
    {
        for (var retry = 0; retry <= _config.OrderRetryMax; retry++)
        {
            if (_cts.IsCancellationRequested)
            {
                Log.Debug("SendParentOrder - canceled");
                return string.Empty;
            }
            var resp = await _client.SendParentOrderAsync(order, _cts.Token);
            if (!resp.IsError)
            {
                var id = resp.GetContent().ParentOrderAcceptanceId;
                _ctx.OrderAccepted(id);
                OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.OrderAccepted, _ctx));
                return id;
            }

            Log.Warn($"SendParentOrder failed: {resp.StatusCode} {resp.ErrorMessage}");
            if (_cts.IsCancellationRequested)
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

    internal BfxTransaction OnParentOrderEvent(BfParentOrderEvent e)
    {
        OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(e, _ctx));
        return this;
    }
    #endregion Parent order

    protected async Task CancelOrderAsync()
    {
        _cts.Cancel();

        if (_ctx.OrderState == BfOrderState.Active)
        {
            _cts.Token.ThrowIfCancellationRequested();
        }

        try
        {
            var resp = await _client.CancelParentOrderAsync(_ctx.ProductCode, string.Empty, _ctx.OrderAcceptanceId, _cts.Token);
            if (!resp.IsError)
            {
            }
            else
            {
            }
        }
        catch (OperationCanceledException ex)
        {
        }
    }
}
