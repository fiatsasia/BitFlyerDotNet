//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using Newtonsoft.Json.Linq;

namespace BitFlyerDotNet.LightningApi
{
    class RealtimeTickerSource : RealtimeSourceBase<BfTicker>
    {
        public readonly string ProductCode;
        Action<RealtimeTickerSource> _dispose;

        internal RealtimeTickerSource(WebSocketChannels channels, string productCode, Action<RealtimeTickerSource> dispose)
            : base(channels, $"lightning_ticker_{productCode}")
        {
            ProductCode = productCode;
            _dispose = dispose;
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _dispose(this);
        }

        public override object OnMessageReceived(JToken token)
        {
            return DispatchMessage(token);
        }
    }
}
