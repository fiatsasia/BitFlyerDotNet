//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Newtonsoft.Json.Linq;

namespace BitFlyerDotNet.LightningApi
{
    class RealtimeBoardSnapshotSource : RealtimeSourceBase<BfBoard>
    {
        public RealtimeBoardSnapshotSource(WebSocketChannels channels, string productCode)
            : base(channels, $"lightning_board_snapshot_{productCode}")
        {
        }

        public override object OnMessageReceived(JToken token)
        {
            return DispatchMessage(token);
        }
    }
}
