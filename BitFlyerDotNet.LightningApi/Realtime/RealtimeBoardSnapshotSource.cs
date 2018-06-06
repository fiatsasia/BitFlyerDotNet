//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PubNubMessaging.Core;
using WebSocket4Net;

namespace BitFlyerDotNet.LightningApi
{
    internal sealed class RealtimeBoardSnapshotSource : RealtimeSourceBase<BfBoard>
    {
        const string ChannelFormat = "lightning_board_snapshot_{0}";

        public RealtimeBoardSnapshotSource(Pubnub pubnub, JsonSerializerSettings jsonSettings, string productCode)
            : base(pubnub, ChannelFormat, jsonSettings, productCode)
        {
        }

        public RealtimeBoardSnapshotSource(WebSocket webSocket, JsonSerializerSettings jsonSettings, string productCode)
            : base(webSocket, ChannelFormat, jsonSettings, productCode)
        {
        }

        protected override void OnPubnubSubscribe(string json)
        {
            json = json.Trim("[]".ToCharArray());
            json = json.Substring(0, json.LastIndexOf("},") + 1);
            OnNext(json);
        }

        public override void OnWebSocketSubscribe(JToken token)
        {
            OnNext(token);
        }
    }
}
