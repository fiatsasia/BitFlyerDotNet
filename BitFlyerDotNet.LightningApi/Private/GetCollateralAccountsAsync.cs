//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfCollateralAccount
{
    [JsonProperty(PropertyName = "currency_code")]
    public string CurrencyCode { get; private set; }

    [JsonProperty(PropertyName = "amount")]
    public decimal Amount { get; private set; }
}

/// <summary>
/// Get Margin Status
/// <see href="https://scrapbox.io/BitFlyerDotNet/GetCollateralAccounts">Online help</see>
/// </summary>
public partial class BitFlyerClient
{
    public Task<BitFlyerResponse<BfCollateralAccount[]>> GetCollateralAccountsAsync(CancellationToken ct)
        => GetPrivateAsync<BfCollateralAccount[]>(nameof(GetCollateralAccountsAsync), string.Empty, ct);

    public async Task<BfCollateralAccount[]> GetCollateralAccountsAsync() => (await GetCollateralAccountsAsync(CancellationToken.None)).GetContent();
}
