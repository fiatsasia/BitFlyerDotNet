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
    public virtual long Id { get; set; }

    [JsonProperty(PropertyName = "currency_code")]
    public virtual string CurrencyCode { get; set; }

    [JsonProperty(PropertyName = "change")]
    public virtual decimal Change { get; set; }

    [JsonProperty(PropertyName = "amount")]
    public virtual decimal Amount { get; set; }

    [JsonProperty(PropertyName = "reason_code")]
    public virtual string ReasonCode { get; set; }

    [JsonProperty(PropertyName = "date")]
    public virtual DateTime Date { get; set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// Get Margin Change History
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetCollateralHistory">Online help</see>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<T[]>> GetCollateralHistoryAsync<T>(long count, long before, long after, CancellationToken ct) where T : BfCollateralHistory
    {
        var query = string.Format("{0}{1}{2}",
            (count > 0)  ? $"&count={count}"   : "",
            (before > 0) ? $"&before={before}" : "",
            (after > 0)  ? $"&after={after}"   : ""
        ).TrimStart('&');

        return GetPrivateAsync<T[]>(nameof(GetCollateralHistoryAsync), query, ct);
    }

    /// <summary>
    /// Get Margin Change History
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetCollateralHistory">Online help</see>
    /// </summary>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfCollateralHistory[]>> GetCollateralHistoryAsync(long count, long before, long after, CancellationToken ct)
        => GetCollateralHistoryAsync<BfCollateralHistory>(count, before, after, ct);

    /// <summary>
    /// Get Margin Change History
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetCollateralHistory">Online help</see>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public async Task<T[]> GetCollateralHistoryAsync<T>(long count = 0L, long before = 0L, long after = 0L) where T : BfCollateralHistory
        => (await GetCollateralHistoryAsync<T>(count, before, after, CancellationToken.None)).Deserialize();

    /// <summary>
    /// Get Margin Change History
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetCollateralHistory">Online help</see>
    /// </summary>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public async Task<BfCollateralHistory[]> GetCollateralHistoryAsync(long count = 0L, long before = 0L, long after = 0L)
        => (await GetCollateralHistoryAsync<BfCollateralHistory>(count, before, after, CancellationToken.None)).Deserialize();
}
