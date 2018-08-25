//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if PUBNUB && DOTNETFRAMEWORK
using PubNubMessaging.Core;
#endif
using WebSocket4Net;

namespace BitFlyerDotNet.LightningApi
{
    internal sealed class RealtimeBoardSnapshotSource : RealtimeSourceBase<BfBoard>
    {
        const string ChannelFormat = "lightning_board_snapshot_{0}";

#if PUBNUB && DOTNETFRAMEWORK
        public RealtimeBoardSnapshotSource(Pubnub pubnub, JsonSerializerSettings jsonSettings, string productCode)
            : base(pubnub, ChannelFormat, jsonSettings, productCode)
        {
        }
#endif

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
