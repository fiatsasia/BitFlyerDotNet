//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Newtonsoft.Json.Linq;

namespace BitFlyerDotNet.LightningApi
{
    interface IRealtimeSource
    {
        string ChannelName { get; }
        object OnMessageReceived(JToken token);
        void Subscribe();
    }
}
