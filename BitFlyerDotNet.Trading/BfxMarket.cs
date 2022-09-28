//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

#pragma warning disable CS8603
#pragma warning disable CS8604

namespace BitFlyerDotNet.Trading;

class BfxMarket
{
    public bool IsInitialized { get; private set; }
    public event EventHandler<BfxOrderChangedEventArgs>? OrderChanged;

    string _productCode;
    BfxApplication _app;

    ConcurrentDictionary<string, BfxTransaction> _tx = new();

    public BfxMarket(BfxApplication app, string productCode)
    {
        _app = app;
        _productCode = productCode;
    }

    public async Task InitializeAsync()
    {
        if (!_app.Client.IsAuthenticated)
        {
            throw new InvalidOperationException("Client is not authorized. To Authenticate first.");
        }
        if (IsInitialized)
        {
            throw new InvalidOperationException($"Market '{_productCode}' is already initialized.");
        }

        IsInitialized = true;

        await foreach (var ctx in _app.DataSource.GetActiveOrderContextsAsync(_productCode))
        {
            _tx.TryAdd(ctx.OrderAcceptanceId, new BfxTransaction(_app, ctx));
        }
    }

    public async Task<string> PlaceOrderAsync<TOrder>(TOrder order, CancellationToken ct) where TOrder : IBfOrder
    {
        // Sometimes child order event arrives before send order process completion.
        var tx = new BfxTransaction(_app, _app.DataSource.CreateOrderContext(_productCode, order.GetOrderType()).Update(order));
        tx.OrderChanged += OnOrderChanged;
        var acceptanceId = await tx.PlaceOrderAsync(order, ct);
        if (string.IsNullOrEmpty(acceptanceId))
        {
            return default;
        }
        Log.Debug($"Transaction id:{acceptanceId} opened");
        _tx.TryAdd(acceptanceId, tx);
        return acceptanceId;
    }

    public async Task CancelOrderAsync(string acceptanceId, CancellationToken ct) => await _tx[acceptanceId].CancelOrderAsync(ct);

    public void OnOrderEvent(IBfOrderEvent e)
    {
        var acceptanceId = e.GetAcceptanceId();
        var tx = _tx.GetOrAdd(acceptanceId, _ =>
        {
            var tx = new BfxTransaction(_app, _app.DataSource.GetOrCreateOrderContext(_productCode, acceptanceId));
            tx.OrderChanged += OnOrderChanged;
            Log.Debug($"Transaction id:{acceptanceId} opened");
            return tx;
        });
        tx.GetOrderContext().Update(e).ContextUpdated();
        tx.OnOrderEvent(e);

        if (!tx.GetOrderContext().IsActive)
        {
            _tx.TryRemove(acceptanceId, out tx);
            Log.Debug($"Transaction id:{acceptanceId} closed");
        }
    }

    void OnOrderChanged(object sender, BfxOrderChangedEventArgs e) => OrderChanged?.Invoke(sender, e);
}
