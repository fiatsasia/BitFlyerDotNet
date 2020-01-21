//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BitFlyerDotNet.LightningApi
{
    internal sealed class RealtimeBoardSnapshotSource : RealtimeSourceBase<BfBoard>
    {
        const string ChannelFormat = "lightning_board_snapshot_{0}";

        public RealtimeBoardSnapshotSource(WebSocketChannels channels, JsonSerializerSettings jsonSettings, string productCode)
            : base(channels, ChannelFormat, jsonSettings, productCode)
        {
        }

        public override void OnSubscribe(JToken token)
        {
            OnNext(token);
        }
    }
}
