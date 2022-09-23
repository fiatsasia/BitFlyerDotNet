//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfDeposit : IBfPagingElement
{
    [JsonProperty(PropertyName = "id")]
    public virtual long Id { get; set; }

    [JsonProperty(PropertyName = "order_id")]
    public virtual string OrderId { get; set; }

    [JsonProperty(PropertyName = "currency_code")]
    public virtual string CurrencyCode { get; set; }

    [JsonProperty(PropertyName = "amount")]
    public virtual decimal Amount { get; set; }

    [JsonProperty(PropertyName = "status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public virtual BfTransactionStatus Status { get; set; }

    [JsonProperty(PropertyName = "event_date")]
    public virtual DateTime EventDate { get; set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// Get Cash Deposits
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetDeposits">Online help</see>
    /// </summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<T[]>> GetDepositsAsync<T>(long count, long before, long after, CancellationToken ct) where T : BfDeposit
    {
        var query = string.Format("{0}{1}{2}",
            (count > 0)  ? $"&count={count}"   : "",
            (before > 0) ? $"&before={before}" : "",
            (after > 0)  ? $"&after={after}"   : ""
        ).TrimStart('&');

        return GetPrivateAsync<T[]>(nameof(GetDepositsAsync), query, ct);
    }

    /// <summary>
    /// Get Cash Deposits
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetDeposits">Online help</see>
    /// </summary>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfDeposit[]>> GetDepositsAsync(long count, long before, long after, CancellationToken ct)
        => GetDepositsAsync<BfDeposit>(count, before, after, ct);

    /// <summary>
    /// Get Cash Deposits
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetDeposits">Online help</see>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public async Task<T[]> GetDepositsAsync<T>(long count = 0L, long before = 0L, long after = 0L) where T : BfDeposit
        => (await GetDepositsAsync<T>(count, before, after, CancellationToken.None)).Deserialize();

    /// <summary>
    /// Get Cash Deposits
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetDeposits">Online help</see>
    /// </summary>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public async Task<BfDeposit[]> GetDepositsAsync(long count = 0L, long before = 0L, long after = 0L)
        => (await GetDepositsAsync<BfDeposit>(count, before, after, CancellationToken.None)).Deserialize();
}
