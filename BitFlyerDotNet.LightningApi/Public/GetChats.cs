//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            return Get<BfChat[]>(nameof(GetChats), string.Format("from_date={0}", fromDate.ToString("yyyy-MM-ddTHH:mm:ss.fff")));
        }
    }
}
