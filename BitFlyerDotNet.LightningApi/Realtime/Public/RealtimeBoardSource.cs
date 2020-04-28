//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocket4Net;

namespace BitFlyerDotNet.LightningApi
{
    internal sealed class RealtimeBoardSource : RealtimeSourceBase<BfBoard>
    {
        const string ChannelFormat = "lightning_board_{0}";

        public RealtimeBoardSource(WebSocketChannels channels, JsonSerializerSettings jsonSettings, string productCode)
            : base(channels, ChannelFormat, jsonSettings, productCode)
        {
        }

        public override void OnSubscribe(JToken token)
        {
            OnNext(token);
        }
    }
}
