//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfBalance
{
    [JsonProperty(PropertyName = "currency_code")]
    public string CurrencyCode { get; private set; }

    [JsonProperty(PropertyName = "amount")]
    public decimal Amount { get; private set; }

    [JsonProperty(PropertyName = "available")]
    public decimal Available { get; private set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// Get Account Asset Balance
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetBalance">Online help</see>
    /// </summary>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfBalance[]>> GetBalanceAsync(CancellationToken ct) => GetPrivateAsync<BfBalance[]>(nameof(GetBalanceAsync), string.Empty, ct);

    public async Task<BfBalance[]> GetBalanceAsync() => (await GetBalanceAsync(CancellationToken.None)).Deserialize();
}
