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
    internal sealed class RealtimeTickerSource : RealtimeSourceBase<BfTicker>
    {
        const string ChannelFormat = "lightning_ticker_{0}";

        internal RealtimeTickerSource(Pubnub pubnub, JsonSerializerSettings jsonSettings, string productCode)
            : base(pubnub, ChannelFormat, jsonSettings, productCode)
        {
        }

        internal RealtimeTickerSource(WebSocket webSocket, JsonSerializerSettings jsonSettings, string productCode)
            : base(webSocket, ChannelFormat, jsonSettings, productCode)
        {
        }

        protected override void OnPubnubSubscribe(string json)
        {
            var executions = Regex.Matches(json, "{.+?}");
            if (executions.Count != 1)
            {
                throw new Exception();
            }
            OnNext(executions[0].Value);
        }

        public override void OnWebSocketSubscribe(JToken token)
        {
            OnNext(token);
        }
    }
}
