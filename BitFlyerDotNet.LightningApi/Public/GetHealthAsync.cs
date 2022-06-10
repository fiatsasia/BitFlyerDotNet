//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfMarketHealth
{
    [JsonProperty(PropertyName = "status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public BfHealthState Status { get; private set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// Exchange status
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetMarketHealth">Online help</see>
    /// </summary>
    /// <param name="productCode"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfMarketHealth>> GetHealthAsync(string productCode, CancellationToken ct)
        => GetAsync<BfMarketHealth>(nameof(GetHealthAsync), "product_code=" + productCode, ct);

    public async Task<BfMarketHealth> GetHealthAsync(string productCode) => (await GetHealthAsync(productCode, CancellationToken.None)).GetContent();
}
