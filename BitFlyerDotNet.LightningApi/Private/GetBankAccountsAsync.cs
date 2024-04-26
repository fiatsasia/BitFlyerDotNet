//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfBankAccount
{
    [JsonProperty(PropertyName = "id")]
    public int AccountId { get; private set; }

    [JsonProperty(PropertyName = "is_verified")]
    public bool IsVerified { get; private set; }

    [JsonProperty(PropertyName = "bank_name")]
    public string BankName { get; private set; }

    [JsonProperty(PropertyName = "branch_name")]
    public string BranchName { get; private set; }

    [JsonProperty(PropertyName = "account_type")]
    public string AccountType { get; private set; }

    [JsonProperty(PropertyName = "account_number")]
    public string AccountNumber { get; private set; }

    [JsonProperty(PropertyName = "account_name")]
    public string AccountName { get; private set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// Get Summary of Bank Accounts
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetBankAccounts">Online help</see>
    /// </summary>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfBankAccount[]>> GetBankAccountsAsync(CancellationToken ct) => GetPrivateAsync<BfBankAccount[]>(nameof(GetBankAccountsAsync), string.Empty, ct);

    public async Task<BfBankAccount[]> GetBankAccountsAsync() => (await GetBankAccountsAsync(CancellationToken.None)).Deserialize();
}
