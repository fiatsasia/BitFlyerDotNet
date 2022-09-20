//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfWithdrawRequest
{
    public string CurrencyCode { get; set; }

    public int BankAccountId { get; set; }

    [JsonConverter(typeof(DecimalJsonConverter))]
    public decimal Amount { get; set; }

    [JsonProperty(PropertyName = "code")]
    public string AuthenticationCode { get; set; }
    public bool ShouldSerializeAuthenticationCode() { return !string.IsNullOrEmpty(AuthenticationCode); }
}

public class BfWithdrawResponse
{
    [JsonProperty(PropertyName = "message_id")]
    public string MessageId { get; private set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// Withdrawing Funds
    /// <see href="https://scrapbox.io/BitFlyerDotNet/Withdraw">Online help</see>
    /// </summary>
    /// <param name="currencyCode"></param>
    /// <param name="bankAccountId"></param>
    /// <param name="amount"></param>
    /// <param name="authenticationCode"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfWithdrawResponse>> WithdrawAsync(string currencyCode, int bankAccountId, decimal amount, string authenticationCode, CancellationToken ct)
    {
        return PostPrivateAsync<BfWithdrawResponse>(nameof(WithdrawAsync), new BfWithdrawRequest
        {
            CurrencyCode = currencyCode,
            BankAccountId = bankAccountId,
            Amount = amount,
            AuthenticationCode = authenticationCode,
        }, ct);
    }

    public async Task<BfWithdrawResponse> WithdrawAsync(string currencyCode, int bankAccountId, decimal amount, string authenticationCode)
        => (await WithdrawAsync(currencyCode, bankAccountId, amount, authenticationCode, CancellationToken.None)).Deserialize();
}
