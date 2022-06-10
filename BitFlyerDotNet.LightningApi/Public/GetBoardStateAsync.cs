//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfBoardStateResult
{
    [JsonProperty(PropertyName = "health")]
    [JsonConverter(typeof(StringEnumConverter))]
    public BfHealthState Health { get; private set; }

    [JsonProperty(PropertyName = "state")]
    [JsonConverter(typeof(StringEnumConverter))]
    public BfBoardState State { get; private set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// Exchange status details
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetBoardState">Online help</see>
    /// </summary>
    /// <param name="productCode"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfBoardStateResult>> GetBoardStateAsync(string productCode, CancellationToken ct)
        => GetAsync<BfBoardStateResult>(nameof(GetBoardStateAsync), "product_code=" + productCode, ct);

    public async Task<BfBoardStateResult> GetBoardStateAsync(string productCode)
        => (await GetBoardStateAsync(productCode, CancellationToken.None)).GetContent();
}
