//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfCollateralHistory : IBfPagingElement
{
    [JsonProperty(PropertyName = "id")]
    public long Id { get; private set; }

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
    public Task<BitFlyerResponse<BfCollateralHistory[]>> GetCollateralHistoryAsync(long count, long before, long after, CancellationToken ct)
    {
        var query = string.Format("{0}{1}{2}",
            (count > 0)  ? $"&count={count}"   : "",
            (before > 0) ? $"&before={before}" : "",
            (after > 0)  ? $"&after={after}"   : ""
        ).TrimStart('&');

        return GetPrivateAsync<BfCollateralHistory[]>(nameof(GetCollateralHistoryAsync), query, ct);
    }

    public async Task<BfCollateralHistory[]> GetCollateralHistoryAsync(long count = 0L, long before = 0L, long after = 0L)
        => (await GetCollateralHistoryAsync(count, before, after, CancellationToken.None)).GetContent();
}
