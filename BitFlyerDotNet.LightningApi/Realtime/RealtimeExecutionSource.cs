//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if PUBNUB && DOTNETFRAMEWORK
using PubNubMessaging.Core;
#endif
using WebSocket4Net;

namespace BitFlyerDotNet.LightningApi
{
    internal sealed class RealtimeExecutionSource : RealtimeSourceBase<BfExecution>
    {
        const string ChannelFormat = "lightning_executions_{0}";

#if PUBNUB && DOTNETFRAMEWORK
        public RealtimeExecutionSource(Pubnub pubnub, JsonSerializerSettings jsonSettings, string productCode)
            : base(pubnub, ChannelFormat, jsonSettings, productCode)
        {
        }
#endif

        public RealtimeExecutionSource(WebSocket webSocket, JsonSerializerSettings jsonSettings, string productCode)
            : base(webSocket, ChannelFormat, jsonSettings, productCode)
        {
        }

        protected override void OnPubnubSubscribe(string json)
        {
            foreach (Match execution in Regex.Matches(json, "{.+?}"))
            {
                OnNext(execution.Value);
            }
        }

        public override void OnWebSocketSubscribe(JToken token)
        {
            OnNextArray(token);
        }
    }
}
