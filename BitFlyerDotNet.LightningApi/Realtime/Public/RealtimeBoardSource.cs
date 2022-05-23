﻿//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using Newtonsoft.Json.Linq;

namespace BitFlyerDotNet.LightningApi
{
    class RealtimeBoardSource : RealtimeSourceBase<BfBoard>
    {
        public RealtimeBoardSource(WebSocketChannels channels, string productCode)
            : base(channels, $"lightning_board_{productCode}")
        {
        }

        public override object OnMessageReceived(JToken token)
        {
            return DispatchMessage(token);
        }
    }
}
