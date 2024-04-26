//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfCoinAddress
{
    [JsonProperty(PropertyName = "type")]
    public string AddressType { get; private set; }

    [JsonProperty(PropertyName = "currency_code")]
    public string CurrencyCode { get; private set; }

    [JsonProperty(PropertyName = "address")]
    public string Address { get; private set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// Get Crypto Assets Deposit Addresses
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetCoinAddresses">Online help</see>
    /// </summary>
    /// <returns></returns>
    /// <summary>
    /// Get Crypto Assets Deposit Addresses
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetCoinAddresses">Online help</see>
    /// </summary>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfCoinAddress[]>> GetAddressesAsync(CancellationToken ct)
        => GetPrivateAsync<BfCoinAddress[]>(nameof(GetAddressesAsync), string.Empty, ct);

    public async Task<BfCoinAddress[]> GetAddressesAsync() => (await GetAddressesAsync(CancellationToken.None)).Deserialize();
}
