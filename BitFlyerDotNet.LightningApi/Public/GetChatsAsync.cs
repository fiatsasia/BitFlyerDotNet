//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

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
    public Task<BitFlyerResponse<BfChat[]>> GetChatsAsync(CancellationToken ct) => GetAsync<BfChat[]>(nameof(GetChatsAsync), string.Empty, ct);

    public async Task<BfChat[]> GetChatsAsync() => (await GetChatsAsync(CancellationToken.None)).GetContent();

    /// <summary>
    /// Chat (Japan)
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChats">Online help</see>
    /// </summary>
    /// <param name="fromDate"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfChat[]>> GetChatsAsync(DateTime fromDate, CancellationToken ct)
    {
        return GetAsync<BfChat[]>(nameof(GetChatsAsync), "from_date=" + fromDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"), ct);
    }

    public async Task<BfChat[]> GetChatsAsync(DateTime fromDate) => (await GetChatsAsync(fromDate, CancellationToken.None)).GetContent();

    /// <summary>
    /// Chat (U.S.)
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChats">Online help</see>
    /// </summary>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfChat[]>> GetChatsUsaAsync(CancellationToken ct) => GetAsync<BfChat[]>(nameof(GetChatsAsync) + UsaMarket, string.Empty, ct);

    public async Task<BfChat[]> GetChatsUsaAsync() => (await GetChatsUsaAsync(CancellationToken.None)).GetContent();

    /// <summary>
    /// Chat (U.S.)
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChats">Online help</see>
    /// </summary>
    /// <param name="fromDate"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfChat[]>> GetChatsUsaAsync(DateTime fromDate, CancellationToken ct)
        => GetAsync<BfChat[]>(nameof(GetChatsAsync) + UsaMarket, "from_date=" + fromDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"), ct);

    public async Task<BfChat[]> GetChatsUsaAsync(DateTime fromDate) => (await GetChatsUsaAsync(fromDate, CancellationToken.None)).GetContent();

    /// <summary>
    /// Chat (E.U.)
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChats">Online help</see>
    /// </summary>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfChat[]>> GetChatsEuAsync(CancellationToken ct) => GetAsync<BfChat[]>(nameof(GetChatsAsync) + EuMarket, string.Empty, ct);

    public async Task<BfChat[]> GetChatsEuAsync() => (await GetChatsEuAsync(CancellationToken.None)).GetContent();

    /// <summary>
    /// Chat (E.U.)
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChats">Online help</see>
    /// </summary>
    /// <param name="fromDate"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfChat[]>> GetChatsEuAsync(DateTime fromDate, CancellationToken ct)
        => GetAsync<BfChat[]>(nameof(GetChatsAsync) + EuMarket, "from_date=" + fromDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"), ct);

    public async Task<BfChat[]> GetChatsEuAsync(DateTime fromDate) => (await GetChatsEuAsync(fromDate, CancellationToken.None)).GetContent();
}
