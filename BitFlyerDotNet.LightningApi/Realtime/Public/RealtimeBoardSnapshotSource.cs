//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

class RealtimeBoardSnapshotSource : RealtimeSourceBase<BfBoard>
{
    public RealtimeBoardSnapshotSource(WebSocketChannel channels, string productCode)
        : base(channels, $"lightning_board_snapshot_{productCode}")
    {
    }

    public override object OnMessageReceived(JToken token) => DispatchMessage(token);
}
