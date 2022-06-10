//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfBalanceHistory
{
    [JsonProperty(PropertyName = "id")]
    public int PagingId { get; private set; }

    [JsonProperty(PropertyName = "trade_date")]
    public DateTime TradeDate { get; private set; }

    [JsonProperty(PropertyName = "event_date")]
    public DateTime EventDate { get; private set; }

    [JsonProperty(PropertyName = "product_code")]
    public string ProductCode { get; private set; }

    [JsonProperty(PropertyName = "currency_code")]
    [JsonConverter(typeof(StringEnumConverter))]
    public BfCurrencyCode CurrencyCode { get; private set; }

    [JsonProperty(PropertyName = "trade_type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public BfTradeType TradeType { get; private set; }

    [JsonProperty(PropertyName = "price")]
    public decimal Price { get; private set; }

    [JsonProperty(PropertyName = "amount")]
    public decimal Amount { get; private set; }

    [JsonProperty(PropertyName = "quantity")]
    public decimal Quantity { get; private set; }

    [JsonProperty(PropertyName = "commission")]
    public decimal Commission { get; private set; }

    [JsonProperty(PropertyName = "balance")]
    public decimal Balance { get; private set; }

    [JsonProperty(PropertyName = "order_id")]
    public string OrderId { get; private set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// List Balance History
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetBalanceHistory">Online help</see>
    /// </summary>
    /// <param name="currencyCode"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfBalanceHistory[]>> GetBalanceHistoryAsync(BfCurrencyCode currencyCode, int count, int before, int after, CancellationToken ct)
    {
        var query = string.Format("currency_code={0}{1}{2}{3}",
            currencyCode.ToEnumString(),
            (count > 0) ? $"&count={count}" : "",
            (before > 0) ? $"&before={before}" : "",
            (after > 0) ? $"&after={after}" : ""
        );
        return GetPrivateAsync<BfBalanceHistory[]>(nameof(GetBalanceHistoryAsync), query, ct);
    }

    public async Task<BfBalanceHistory[]> GetBalanceHistoryAsync(BfCurrencyCode currencyCode, int count = 0, int before = 0, int after = 0)
        => (await GetBalanceHistoryAsync(currencyCode, count, before, after, CancellationToken.None)).GetContent();

    public async IAsyncEnumerable<BfBalanceHistory> GetBalanceHistoryAsync(BfCurrencyCode currencyCode, int before, Func<BfBalanceHistory, bool> predicate)
    {
        while (true)
        {
            var balances = (await GetBalanceHistoryAsync(currencyCode, ReadCountMax, before, 0, CancellationToken.None)).GetContent();
            if (balances.Length == 0)
            {
                break;
            }

            foreach (var balance in balances)
            {
                if (!predicate(balance))
                {
                    yield break;
                }
                yield return balance;
            }

            if (balances.Length < ReadCountMax)
            {
                break;
            }
            before = balances.Last().PagingId;
        }
    }
}
