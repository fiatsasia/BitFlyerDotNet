//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfBalanceHistory : IBfPagingElement
{
    [JsonProperty(PropertyName = "id")]
    public virtual long Id { get; set; }

    [JsonProperty(PropertyName = "trade_date")]
    public virtual DateTime TradeDate { get; set; }

    [JsonProperty(PropertyName = "event_date")]
    public virtual DateTime EventDate { get; set; }

    [JsonProperty(PropertyName = "product_code")]
    public virtual string ProductCode { get; set; }

    [JsonProperty(PropertyName = "currency_code")]
    public virtual string CurrencyCode { get; set; }

    [JsonProperty(PropertyName = "trade_type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public virtual BfTradeType TradeType { get; set; }

    [JsonProperty(PropertyName = "price")]
    public virtual decimal Price { get; set; }

    [JsonProperty(PropertyName = "amount")]
    public virtual decimal Amount { get; set; }

    [JsonProperty(PropertyName = "quantity")]
    public virtual decimal Quantity { get; set; }

    [JsonProperty(PropertyName = "commission")]
    public virtual decimal Commission { get; set; }

    [JsonProperty(PropertyName = "balance")]
    public virtual decimal Balance { get; set; }

    [JsonProperty(PropertyName = "order_id")]
    public virtual string OrderId { get; set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// List Balance History
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetBalanceHistory">Online help</see>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="currencyCode"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<T[]>> GetBalanceHistoryAsync<T>(string currencyCode, long count, long before, long after, CancellationToken ct) where T : BfBalanceHistory
    {
        var query = string.Format("currency_code={0}{1}{2}{3}",
            currencyCode,
            (count > 0) ? $"&count={count}" : "",
            (before > 0) ? $"&before={before}" : "",
            (after > 0) ? $"&after={after}" : ""
        );
        return GetPrivateAsync<T[]>(nameof(GetBalanceHistoryAsync), query, ct);
    }

    /// <summary>
    /// List Balance History
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetBalanceHistory">Online help</see>
    /// </summary>
    /// <param name="currencyCode"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfBalanceHistory[]>> GetBalanceHistoryAsync(string currencyCode, long count, long before, long after, CancellationToken ct)
        => GetBalanceHistoryAsync<BfBalanceHistory>(currencyCode, count, before, after, ct);

    /// <summary>
    /// List Balance History
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetBalanceHistory">Online help</see>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="currencyCode"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public async Task<T[]> GetBalanceHistoryAsync<T>(string currencyCode, long count = 0, long before = 0, long after = 0) where T : BfBalanceHistory
        => (await GetBalanceHistoryAsync<T>(currencyCode, count, before, after, CancellationToken.None)).Deserialize();

    /// <summary>
    /// List Balance History
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetBalanceHistory">Online help</see>
    /// </summary>
    /// <param name="currencyCode"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public async Task<BfBalanceHistory[]> GetBalanceHistoryAsync(string currencyCode, long count = 0, long before = 0, long after = 0)
        => (await GetBalanceHistoryAsync<BfBalanceHistory>(currencyCode, count, before, after, CancellationToken.None)).Deserialize();
}
