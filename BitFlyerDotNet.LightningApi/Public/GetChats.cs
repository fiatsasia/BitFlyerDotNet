//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using Newtonsoft.Json;

namespace BitFlyerDotNet.LightningApi
{
    public class BfChat
    {
        [JsonProperty(PropertyName = "nickname")]
        public string Nickname { get; private set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; private set; }

        [JsonProperty(PropertyName = "date")]
        public DateTime Date { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfChat[]> GetChats()
        {
            return Get<BfChat[]>(nameof(GetChats));
        }

        public BitFlyerResponse<BfChat[]> GetChats(DateTime fromDate)
        {
            return Get<BfChat[]>(nameof(GetChats), "from_date=" + fromDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
        }

        public BitFlyerResponse<BfChat[]> GetChatsUsa()
        {
            return Get<BfChat[]>(nameof(GetChats) + _usaMarket);
        }

        public BitFlyerResponse<BfChat[]> GetChatsUsa(DateTime fromDate)
        {
            return Get<BfChat[]>(nameof(GetChats) + _usaMarket, "from_date=" + fromDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
        }

        public BitFlyerResponse<BfChat[]> GetChatsEu()
        {
            return Get<BfChat[]>(nameof(GetChats) + _euMarket);
        }

        public BitFlyerResponse<BfChat[]> GetChatsEu(DateTime fromDate)
        {
            return Get<BfChat[]>(nameof(GetChats) + _euMarket, "from_date=" + fromDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
        }
    }
}
