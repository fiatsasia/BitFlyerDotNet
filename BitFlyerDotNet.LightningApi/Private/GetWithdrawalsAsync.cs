//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfWithdrawal : IBfPagingElement
{
    [JsonProperty(PropertyName = "id")]
    public long Id { get; private set; }

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
    /// Get Deposit Cancellation History
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetWithdrawals">Online help</see>
    /// </summary>
    /// <param name="messageId"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfWithdrawal[]>> GetWithdrawalsAsync(string messageId, long count, long before, long after, CancellationToken ct)
    {
        var query = string.Format("{0}{1}{2}{3}",
            !string.IsNullOrEmpty(messageId) ? "message_id=" + messageId : "",
            (count > 0)  ? $"&count={count}"   : "",
            (before > 0) ? $"&before={before}" : "",
            (after > 0)  ? $"&after={after}"   : ""
        ).TrimStart('&');

        return GetPrivateAsync<BfWithdrawal[]>(nameof(GetWithdrawalsAsync), query, ct);
    }

    public async Task<BfWithdrawal[]> GetWithdrawalsAsync(string messageId = null, long count = 0L, long before = 0L, long after = 0L)
        => (await GetWithdrawalsAsync(messageId, count, before, after, CancellationToken.None)).Deserialize();
}
