//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
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
        /// <summary>
        /// Chat (Japan)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChats">Online help</see>
        /// </summary>
        /// <returns></returns>
        public BitFlyerResponse<BfChat[]> GetChats()
        {
            return Get<BfChat[]>(nameof(GetChats));
        }

        /// <summary>
        /// Chat (Japan)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChats">Online help</see>
        /// </summary>
        /// <param name="fromDate"></param>
        /// <returns></returns>
        public BitFlyerResponse<BfChat[]> GetChats(DateTime fromDate)
        {
            return Get<BfChat[]>(nameof(GetChats), "from_date=" + fromDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
        }

        /// <summary>
        /// Chat (U.S.)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChats">Online help</see>
        /// </summary>
        /// <returns></returns>
        public BitFlyerResponse<BfChat[]> GetChatsUsa()
        {
            return Get<BfChat[]>(nameof(GetChats) + UsaMarket);
        }

        /// <summary>
        /// Chat (U.S.)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChats">Online help</see>
        /// </summary>
        /// <param name="fromDate"></param>
        /// <returns></returns>
        public BitFlyerResponse<BfChat[]> GetChatsUsa(DateTime fromDate)
        {
            return Get<BfChat[]>(nameof(GetChats) + UsaMarket, "from_date=" + fromDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
        }

        /// <summary>
        /// Chat (E.U.)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChats">Online help</see>
        /// </summary>
        /// <returns></returns>
        public BitFlyerResponse<BfChat[]> GetChatsEu()
        {
            return Get<BfChat[]>(nameof(GetChats) + EuMarket);
        }

        /// <summary>
        /// Chat (E.U.)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChats">Online help</see>
        /// </summary>
        /// <param name="fromDate"></param>
        /// <returns></returns>
        public BitFlyerResponse<BfChat[]> GetChatsEu(DateTime fromDate)
        {
            return Get<BfChat[]>(nameof(GetChats) + EuMarket, "from_date=" + fromDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
        }
    }
}
