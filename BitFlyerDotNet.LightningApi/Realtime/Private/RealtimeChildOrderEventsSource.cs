//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using Newtonsoft.Json.Linq;

namespace BitFlyerDotNet.LightningApi
{
    class RealtimeChildOrderEventsSource : RealtimeSourceBase<BfChildOrderEvent>
    {
        Action<RealtimeChildOrderEventsSource> _dispose;

        public RealtimeChildOrderEventsSource(WebSocketChannels channels, Action<RealtimeChildOrderEventsSource> dispose)
            : base(channels, "child_order_events")
        {
            _dispose = dispose;
        }

        public override object OnMessageReceived(JToken token)
        {
            return DispatchArrayMessage(token); // Channel returns array format
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _dispose(this);
        }
    }
}
