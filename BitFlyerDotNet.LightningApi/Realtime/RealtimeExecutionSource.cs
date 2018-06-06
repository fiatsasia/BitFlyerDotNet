//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PubNubMessaging.Core;
using WebSocket4Net;

namespace BitFlyerDotNet.LightningApi
{
    internal sealed class RealtimeExecutionSource : RealtimeSourceBase<BfExecution>
    {
        const string ChannelFormat = "lightning_executions_{0}";

        public RealtimeExecutionSource(Pubnub pubnub, JsonSerializerSettings jsonSettings, string productCode)
            : base(pubnub, ChannelFormat, jsonSettings, productCode)
        {
        }

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
