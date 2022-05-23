//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Threading;
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
        /// <summary>
        /// Chat (Japan)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChats">Online help</see>
        /// </summary>
        /// <returns></returns>
        public Task<BitFlyerResponse<BfChat[]>> GetChatsAsync(CancellationToken ct) => GetAsync<BfChat[]>(nameof(GetChats), string.Empty, ct);

        public BitFlyerResponse<BfChat[]> GetChats() => GetChatsAsync(CancellationToken.None).Result;

        /// <summary>
        /// Chat (Japan)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChats">Online help</see>
        /// </summary>
        /// <param name="fromDate"></param>
        /// <returns></returns>
        public Task<BitFlyerResponse<BfChat[]>> GetChatsAsync(DateTime fromDate, CancellationToken ct)
        {
            return GetAsync<BfChat[]>(nameof(GetChats), "from_date=" + fromDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"), ct);
        }

        public BitFlyerResponse<BfChat[]> GetChats(DateTime fromDate) => GetChatsAsync(fromDate, CancellationToken.None).Result;

        /// <summary>
        /// Chat (U.S.)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChats">Online help</see>
        /// </summary>
        /// <returns></returns>
        public Task<BitFlyerResponse<BfChat[]>> GetChatsUsaAsync(CancellationToken ct) => GetAsync<BfChat[]>(nameof(GetChats) + UsaMarket, string.Empty, ct);

        public BitFlyerResponse<BfChat[]> GetChatsUsa() => GetChatsUsaAsync(CancellationToken.None).Result;

        /// <summary>
        /// Chat (U.S.)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChats">Online help</see>
        /// </summary>
        /// <param name="fromDate"></param>
        /// <returns></returns>
        public Task<BitFlyerResponse<BfChat[]>> GetChatsUsaAsync(DateTime fromDate, CancellationToken ct)
            => GetAsync<BfChat[]>(nameof(GetChats) + UsaMarket, "from_date=" + fromDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"), ct);

        public BitFlyerResponse<BfChat[]> GetChatsUsa(DateTime fromDate) => GetChatsUsaAsync(fromDate, CancellationToken.None).Result;

        /// <summary>
        /// Chat (E.U.)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChats">Online help</see>
        /// </summary>
        /// <returns></returns>
        public Task<BitFlyerResponse<BfChat[]>> GetChatsEu(CancellationToken ct) => GetAsync<BfChat[]>(nameof(GetChats) + EuMarket, string.Empty, ct);

        public BitFlyerResponse<BfChat[]> GetChatsEu() => GetChatsEu(CancellationToken.None).Result;

        /// <summary>
        /// Chat (E.U.)
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChats">Online help</see>
        /// </summary>
        /// <param name="fromDate"></param>
        /// <returns></returns>
        public Task<BitFlyerResponse<BfChat[]>> GetChatsEu(DateTime fromDate, CancellationToken ct)
            => GetAsync<BfChat[]>(nameof(GetChats) + EuMarket, "from_date=" + fromDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"), ct);

        public BitFlyerResponse<BfChat[]> GetChatsEu(DateTime fromDate) => GetChatsEu(fromDate, CancellationToken.None).Result;
    }
}
