//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

class RealtimeTickerSource : RealtimeSourceBase<BfTicker>
{
    public readonly string ProductCode;
    Action<RealtimeTickerSource> _dispose;

    internal RealtimeTickerSource(WebSocketChannel channels, string productCode, Action<RealtimeTickerSource> dispose)
        : base(channels, $"lightning_ticker_{productCode}")
    {
        ProductCode = productCode;
        _dispose = dispose;
    }

    protected override void OnDispose()
    {
        base.OnDispose();
        _dispose(this);
    }

    public override object OnMessageReceived(JToken token) => DispatchMessage(token);
}
