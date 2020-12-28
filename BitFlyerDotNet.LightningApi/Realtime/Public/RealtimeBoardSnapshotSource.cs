//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
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
