﻿//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

class RealtimeExecutionSource : RealtimeSourceBase<BfExecution>
{
    public readonly string ProductCode;
    Action<RealtimeExecutionSource> _dispose;

    public RealtimeExecutionSource(WebSocketChannel channels, string productCode, Action<RealtimeExecutionSource> dispose)
        : base(channels, $"lightning_executions_{productCode}")
    {
        ProductCode = productCode;
        _dispose = dispose;
    }

    public override object OnMessageReceived(JToken token) => DispatchArrayMessage(token); // Channel returns array format

    protected override void OnDispose()
    {
        base.OnDispose();
        _dispose(this);
    }
}
