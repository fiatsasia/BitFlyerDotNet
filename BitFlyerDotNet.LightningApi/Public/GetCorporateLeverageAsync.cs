//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfCorporateLeverage
{
    [JsonProperty(PropertyName = "current_max")]
    public decimal CurrentMax { get; private set; }

    [JsonProperty(PropertyName = "current_startdate")]
    public DateTime CurrentStartDate { get; private set; }

    [JsonProperty(PropertyName = "next_max")]
    public decimal? NextMax { get; private set; }

    [JsonProperty(PropertyName = "next_startdate")]
    public DateTime? NextStartDate { get; private set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// Order Book
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetCorporateLeverage">Online help</see>
    /// </summary>
    /// <param name="productCode"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfCorporateLeverage>> GetCorporateLeverageAsync(CancellationToken ct) => GetAsync<BfCorporateLeverage>(nameof(GetCorporateLeverageAsync), string.Empty, ct);

    public async Task<BfCorporateLeverage> GetCorporateLeverageAsync() => (await GetCorporateLeverageAsync(CancellationToken.None)).Deserialize();
}
