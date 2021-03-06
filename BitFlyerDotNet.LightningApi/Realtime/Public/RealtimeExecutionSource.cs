﻿//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using Newtonsoft.Json.Linq;

namespace BitFlyerDotNet.LightningApi
{
    class RealtimeExecutionSource : RealtimeSourceBase<BfaExecution>
    {
        public readonly string ProductCode;
        Action<RealtimeExecutionSource> _dispose;

        public RealtimeExecutionSource(WebSocketChannels channels, string productCode, Action<RealtimeExecutionSource> dispose)
            : base(channels, $"lightning_executions_{productCode}")
        {
            ProductCode = productCode;
            _dispose = dispose;
        }

        public override object OnMessageReceived(JToken token)
        {
            return DispatchArrayMessage(token); // Channel returns array format
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _dispose(this);
        }
    }
}
