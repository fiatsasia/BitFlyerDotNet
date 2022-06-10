//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfMarket
{
    [JsonProperty(PropertyName = "product_code")]
    public string ProductCode { get; private set; }

    [JsonProperty(PropertyName = "market_type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public BfMarketType MarketType { get; private set; }

    [JsonProperty(PropertyName = "alias")]
    public string Alias { get; private set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// Market List (Japan)
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetMarkets">Online help</see>
    /// </summary>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfMarket[]>> GetMarketsAsync(CancellationToken ct) => GetAsync<BfMarket[]>(nameof(GetMarketsAsync), string.Empty, ct);

    public async Task<BfMarket[]> GetMarketsAsync() => (await GetMarketsAsync(CancellationToken.None)).GetContent();

    /// <summary>
    /// Market List (U.S.)
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetMarkets">Online help</see>
    /// </summary>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfMarket[]>> GetMarketsUsaAsync(CancellationToken ct) => GetAsync<BfMarket[]>(nameof(GetMarketsAsync) + UsaMarket, string.Empty, ct);

    public async Task<BfMarket[]> GetMarketsUsaAsync() => (await GetMarketsUsaAsync(CancellationToken.None)).GetContent();

    /// <summary>
    /// Market List (E.U.)
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetMarkets">Online help</see>
    /// </summary>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfMarket[]>> GetMarketsEuAsync(CancellationToken ct) => GetAsync<BfMarket[]>(nameof(GetMarketsAsync) + EuMarket, string.Empty, ct);

    public async Task<BfMarket[]> GetMarketsEuAsync() => (await GetMarketsEuAsync(CancellationToken.None)).GetContent();
}
