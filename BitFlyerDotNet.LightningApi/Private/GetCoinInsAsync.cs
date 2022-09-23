//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfCoinin : IBfPagingElement
{
    [JsonProperty(PropertyName = "id")]
    public virtual long Id { get; set; }

    [JsonProperty(PropertyName = "order_id")]
    public virtual string OrderId { get; set; }

    [JsonProperty(PropertyName = "currency_code")]
    public virtual string CurrencyCode { get; set; }

    [JsonProperty(PropertyName = "amount")]
    public virtual decimal Amount { get; set; }

    [JsonProperty(PropertyName = "address")]
    public virtual string Address { get; set; }

    [JsonProperty(PropertyName = "tx_hash")]
    public virtual string TxHash { get; set; }

    [JsonProperty(PropertyName = "status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public virtual BfTransactionStatus Status { get; set; }

    [JsonProperty(PropertyName = "event_date")]
    public virtual DateTime EventDate { get; set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// Get Crypto Assets Deposit History
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetCoinIns">Online help</see>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<T[]>> GetCoinInsAsync<T>(long count, long before, long after, CancellationToken ct) where T : BfCoinin
    {
        var query = string.Format("{0}{1}{2}",
            (count > 0)  ? $"&count={count}"   : "",
            (before > 0) ? $"&before={before}" : "",
            (after > 0)  ? $"&after={after}"   : ""
        ).TrimStart('&');

        return GetPrivateAsync<T[]>(nameof(GetCoinInsAsync), query, ct);
    }

    /// <summary>
    /// Get Crypto Assets Deposit History
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetCoinIns">Online help</see>
    /// </summary>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfCoinin[]>> GetCoinInsAsync(long count, long before, long after, CancellationToken ct)
        => GetCoinInsAsync<BfCoinin>(count, before, after, ct);

    /// <summary>
    /// Get Crypto Assets Deposit History
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetCoinIns">Online help</see>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public async Task<T[]> GetCoinInsAsync<T>(long count = 0, long before = 0, long after = 0) where T : BfCoinin
        => (await GetCoinInsAsync<T>(count, before, after, CancellationToken.None)).Deserialize();

    /// <summary>
    /// Get Crypto Assets Deposit History
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetCoinIns">Online help</see>
    /// </summary>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public async Task<BfCoinin[]> GetCoinInsAsync(long count = 0, long before = 0, long after = 0)
        => (await GetCoinInsAsync<BfCoinin>(count, before, after, CancellationToken.None)).Deserialize();
}
