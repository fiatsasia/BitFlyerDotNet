﻿//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfDeposit
{
    [JsonProperty(PropertyName = "id")]
    public int PagingId { get; private set; }

    [JsonProperty(PropertyName = "order_id")]
    public string OrderId { get; private set; }

    [JsonProperty(PropertyName = "currency_code")]
    public string CurrencyCode { get; private set; }

    [JsonProperty(PropertyName = "amount")]
    public decimal Amount { get; private set; }

    [JsonProperty(PropertyName = "status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public BfTransactionStatus TransactionStatus { get; private set; }

    [JsonProperty(PropertyName = "event_date")]
    public DateTime EventDate { get; private set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// Get Cash Deposits
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetDeposits">Online help</see>
    /// </summary>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfDeposit[]>> GetDepositsAsync(int count, int before, int after, CancellationToken ct)
    {
        var query = string.Format("{0}{1}{2}",
            (count > 0)  ? $"&count={count}"   : "",
            (before > 0) ? $"&before={before}" : "",
            (after > 0)  ? $"&after={after}"   : ""
        ).TrimStart('&');

        return GetPrivateAsync<BfDeposit[]>(nameof(GetDepositsAsync), query, ct);
    }

    public async Task<BfDeposit[]> GetDepositsAsync(int count = 0, int before = 0, int after = 0)
        => (await GetDepositsAsync(count, before, after, CancellationToken.None)).GetContent();
}
