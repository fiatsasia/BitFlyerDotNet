//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocket4Net;

namespace BitFlyerDotNet.LightningApi
{
    internal sealed class RealtimeExecutionSource : RealtimeSourceBase<BfExecution>
    {
        const string ChannelFormat = "lightning_executions_{0}";

        public RealtimeExecutionSource(WebSocket webSocket, JsonSerializerSettings jsonSettings, string productCode)
            : base(webSocket, ChannelFormat, jsonSettings, productCode)
        {
        }

        public override void OnSubscribe(JToken token)
        {
            OnNextArray(token);
        }
    }
}
