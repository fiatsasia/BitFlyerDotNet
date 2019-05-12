//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocket4Net;

namespace BitFlyerDotNet.LightningApi
{
    internal sealed class RealtimeBoardSnapshotSource : RealtimeSourceBase<BfBoard>
    {
        const string ChannelFormat = "lightning_board_snapshot_{0}";

        public RealtimeBoardSnapshotSource(WebSocket webSocket, JsonSerializerSettings jsonSettings, string productCode)
            : base(webSocket, ChannelFormat, jsonSettings, productCode)
        {
        }

        public override void OnSubscribe(JToken token)
        {
            OnNext(token);
        }
    }
}
