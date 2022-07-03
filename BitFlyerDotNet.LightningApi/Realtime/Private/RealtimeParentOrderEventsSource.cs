//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

class RealtimeParentOrderEventsSource : RealtimeSourceBase<BfParentOrderEvent>
{
    Action<RealtimeParentOrderEventsSource> _dispose;

    public RealtimeParentOrderEventsSource(WebSocketChannel channels, Action<RealtimeParentOrderEventsSource> dispose)
        : base(channels, "parent_order_events")
    {
        _dispose = dispose;
    }

    public override object OnMessageReceived(JToken token) => DispatchArrayMessage(token); // Channel returns array format

    protected override void OnDispose()
    {
        base.OnDispose();
        _dispose(this);
    }
}
