//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfCollateralHistory
{
    [JsonProperty(PropertyName = "id")]
    public int PagingId { get; private set; }

    [JsonProperty(PropertyName = "currency_code")]
    public string CurrencyCode { get; private set; }

    [JsonProperty(PropertyName = "change")]
    public decimal Change { get; private set; }

    [JsonProperty(PropertyName = "amount")]
    public decimal Amount { get; private set; }

    [JsonProperty(PropertyName = "reason_code")]
    public string ReasonCode { get; private set; }

    [JsonProperty(PropertyName = "date")]
    public DateTime Date { get; private set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// Get Margin Change History
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetCollateralHistory">Online help</see>
    /// </summary>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfCollateralHistory[]>> GetCollateralHistoryAsync(int count, int before, int after, CancellationToken ct)
    {
        var query = string.Format("{0}{1}{2}",
            (count > 0)  ? $"&count={count}"   : "",
            (before > 0) ? $"&before={before}" : "",
            (after > 0)  ? $"&after={after}"   : ""
        ).TrimStart('&');

        return GetPrivateAsync<BfCollateralHistory[]>(nameof(GetCollateralHistoryAsync), query, ct);
    }

    public async Task<BfCollateralHistory[]> GetCollateralHistoryAsync(int count = 0, int before = 0, int after = 0)
        => (await GetCollateralHistoryAsync(count, before, after, CancellationToken.None)).GetContent();

    public async IAsyncEnumerable<BfCollateralHistory> GetCollateralHistoryAsync(int before, Func<BfCollateralHistory, bool> predicate)
    {
        while (true)
        {
            var execs = await GetCollateralHistoryAsync(ReadCountMax, before, 0);
            if (execs.Length == 0)
            {
                break;
            }

            foreach (var exec in execs)
            {
                if (!predicate(exec))
                {
                    yield break;
                }
                yield return exec;
            }

            if (execs.Length < ReadCountMax)
            {
                break;
            }
            before = execs.Last().PagingId;
        }
    }
}
