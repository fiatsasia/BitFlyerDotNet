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
    /// Withdrawing Funds
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetWithdrawals">Online help</see>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="messageId"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<T[]>> GetWithdrawalsAsync<T>(string messageId, long count, long before, long after, CancellationToken ct) where T : BfWithdrawal
    {
        var query = string.Format("{0}{1}{2}{3}",
            !string.IsNullOrEmpty(messageId) ? "message_id=" + messageId : "",
            (count > 0)  ? $"&count={count}"   : "",
            (before > 0) ? $"&before={before}" : "",
            (after > 0)  ? $"&after={after}"   : ""
        ).TrimStart('&');

        return GetPrivateAsync<T[]>(nameof(GetWithdrawalsAsync), query, ct);
    }

    /// <summary>
    /// Withdrawing Funds
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetWithdrawals">Online help</see>
    /// </summary>
    /// <param name="messageId"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfWithdrawal[]>> GetWithdrawalsAsync(string messageId, long count, long before, long after, CancellationToken ct)
        => GetWithdrawalsAsync<BfWithdrawal>(messageId, count, before, after, ct);

    /// <summary>
    /// Withdrawing Funds
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetWithdrawals">Online help</see>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="messageId"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public async Task<T[]> GetWithdrawalsAsync<T>(string messageId = null, long count = 0L, long before = 0L, long after = 0L) where T : BfWithdrawal
        => (await GetWithdrawalsAsync<T>(messageId, count, before, after, CancellationToken.None)).Deserialize();

    /// <summary>
    /// Withdrawing Funds
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetWithdrawals">Online help</see>
    /// </summary>
    /// <param name="messageId"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public async Task<BfWithdrawal[]> GetWithdrawalsAsync(string messageId = null, long count = 0L, long before = 0L, long after = 0L)
        => (await GetWithdrawalsAsync<BfWithdrawal>(messageId, count, before, after, CancellationToken.None)).Deserialize();
}
