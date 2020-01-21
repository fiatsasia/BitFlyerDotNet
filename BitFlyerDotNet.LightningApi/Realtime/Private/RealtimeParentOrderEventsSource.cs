//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocket4Net;

namespace BitFlyerDotNet.LightningApi
{
    internal sealed class RealtimeParentOrderEventsSource : RealtimePrivateSourceBase<BfParentOrderEvent>
    {
        const string ChannelFormat = "parent_order_events";

        public RealtimeParentOrderEventsSource(WebSocketChannels channels, JsonSerializerSettings jsonSettings, string key, string secret)
            : base(channels, ChannelFormat, jsonSettings, key, secret)
        {
        }

        public override void OnSubscribe(JToken token)
        {
            OnNextArray(token);
        }
    }
}
