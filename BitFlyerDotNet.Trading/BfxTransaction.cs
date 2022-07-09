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
    public event EventHandler<BfxOrderChangedEventArgs>? OrderChanged;

    BitFlyerClient _client;
    CancellationTokenSource _cts = new CancellationTokenSource();
    BfxOrderStatus _os;
    BfxConfiguration _config;

    internal BfxTransaction(BitFlyerClient client, string productCode, BfxConfiguration config)
    {
        _client = client;
        _config = config;
        _os = new BfxOrderStatus(productCode);
    }

    #region Child order
    internal BfxTransaction Update(BfChildOrder order)
    {
        _os.Update(order);
        return this;
    }
    public BfxTransaction Update(BfChildOrderStatus status, IEnumerable<BfPrivateExecution> execs)
    {
        _os.Update(status, execs);
        return this;
    }

    internal BfxTransaction OnChildOrderEvent(BfChildOrderEvent e)
    {
        _os.Update(e);
        var et = e.EventType switch
        {
            BfOrderEventType.Order => BfxOrderEventType.OrderAccepted,
            BfOrderEventType.OrderFailed => BfxOrderEventType.OrderFailed,
            BfOrderEventType.Cancel => BfxOrderEventType.Canceled,
            BfOrderEventType.CancelFailed => BfxOrderEventType.CancelFailed,
            BfOrderEventType.Execution => (_os.OrderSize > _os.ExecutedSize)
                ? BfxOrderEventType.PartiallyExecuted
                : BfxOrderEventType.Executed,
            BfOrderEventType.Expire => BfxOrderEventType.Expired,
            _ => throw new ArgumentException()
        };
        OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(et, _os));
        return this;
    }

    public async Task<string> PlaceOrderAsync(BfChildOrder order)
    {
        _os.Update(order);
        for (var retry = 0; retry <= _config.OrderRetryMax; retry++)
        {
            _cts.Token.ThrowIfCancellationRequested();
            var resp = await _client.SendChildOrderAsync(order, _cts.Token);
            if (!resp.IsError)
            {
                var id = resp.GetContent().ChildOrderAcceptanceId;
                _os.OrderAcceptanceId = id;
                OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.OrderAccepted, _os));
                return id;
            }

            Log.Warn($"SendChildOrder failed: {resp.StatusCode} {resp.ErrorMessage}");
            _cts.Token.ThrowIfCancellationRequested();
            Log.Info("Trying retry...");
            OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.RetryingOrder, _os));
            await Task.Delay(_config.OrderRetryInterval);
        }

        Log.Error("SendOrderRequest - Retried out");
        OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.RetriedOut, _os));
        return string.Empty;
    }
    #endregion Child order

    #region Parent order
    internal BfxTransaction Update(BfParentOrder order)
    {
        _os.Update(order);
        return this;
    }
    internal BfxTransaction Update(BfParentOrderStatus status, BfParentOrderDetailStatus detail)
    {
        _os.Update(status, detail);
        return this;
    }

#pragma warning disable CS8629
    internal BfxTransaction OnParentOrderEvent(BfParentOrderEvent e)
    {
        _os.Update(e);
        var et = e.EventType switch
        {
            BfOrderEventType.Order => BfxOrderEventType.OrderAccepted,
            BfOrderEventType.OrderFailed => BfxOrderEventType.OrderFailed,
            BfOrderEventType.Cancel => BfxOrderEventType.Canceled,
            BfOrderEventType.Trigger => BfxOrderEventType.ChildOrderChanged,
            BfOrderEventType.Complete => BfxOrderEventType.ChildOrderChanged,
            BfOrderEventType.Expire => BfxOrderEventType.Expired,
            _ => throw new ArgumentException()
        };
        OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(et, _os));
        return this;
    }
#pragma warning restore CS8629

    // - 経過時間でリトライ終了のオプション
    public async Task<string> PlaceOrdertAsync(BfParentOrder order)
    {
        _os.Update(order);
        for (var retry = 0; retry <= _config.OrderRetryMax; retry++)
        {
            _cts.Token.ThrowIfCancellationRequested();
            var resp = await _client.SendParentOrderAsync(order, _cts.Token);
            if (!resp.IsError)
            {
                var id = resp.GetContent().ParentOrderAcceptanceId;
                _os.OrderAcceptanceId = id;
                OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.OrderAccepted, _os));
                return id;
            }

            _cts.Token.ThrowIfCancellationRequested();
            Log.Info("Trying retry...");
            OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.RetryingOrder, _os));
            await Task.Delay(_config.OrderRetryInterval);
        }

        Log.Error("SendOrderRequest - Retried out");
        OrderChanged?.Invoke(this, new BfxOrderChangedEventArgs(BfxOrderEventType.RetriedOut, _os));
        return string.Empty;
    }
    #endregion Parent order

    protected async Task CancelOrderAsync()
    {
        //protected void CancelTransaction() => _cts.Cancel();

        if (_os.OrderState == BfOrderState.Active)
        {
            _cts.Token.ThrowIfCancellationRequested();
        }

        try
        {
            var resp = await _client.CancelParentOrderAsync(_os.ProductCode, string.Empty, _os.OrderAcceptanceId, _cts.Token);
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
