//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

class RealtimeChildOrderEventsSource : RealtimeSourceBase<BfChildOrderEvent>
{
    Action<RealtimeChildOrderEventsSource> _dispose;

    public RealtimeChildOrderEventsSource(WebSocketChannel channels, Action<RealtimeChildOrderEventsSource> dispose)
        : base(channels, "child_order_events")
    {
        _dispose = dispose;
    }

    public override object OnMessageReceived(JToken token)
    {
        Log.TraceJson("Child order event received:", token.ToString(Formatting.None));
        return DispatchArrayMessage(token); // Channel returns array format
    }

    protected override void OnDispose()
    {
        base.OnDispose();
        _dispose(this);
    }
}
