//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocket4Net;

namespace BitFlyerDotNet.LightningApi
{
    internal sealed class RealtimeChildOrderEventsSource : RealtimePrivateSourceBase<BfChildOrderEvent>
    {
        const string ChannelFormat = "child_order_events";

        public RealtimeChildOrderEventsSource(WebSocketChannels channels, JsonSerializerSettings jsonSettings, string key, string secret)
            : base(channels, ChannelFormat, jsonSettings, key, secret)
        {
        }

        public override void OnSubscribe(JToken token)
        {
            OnNextArray(token);
        }
    }
}
