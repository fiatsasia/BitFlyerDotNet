//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfPosition
{
    [JsonProperty(PropertyName = "product_code")]
    public string ProductCode { get; private set; }

    [JsonProperty(PropertyName = "side")]
    [JsonConverter(typeof(StringEnumConverter))]
    public BfTradeSide Side { get; private set; }

    [JsonProperty(PropertyName = "price")]
    public decimal Price { get; private set; }

    [JsonProperty(PropertyName = "size")]
    public decimal Size { get; private set; }

    [JsonProperty(PropertyName = "commission")]
    public decimal Commission { get; private set; }

    [JsonProperty(PropertyName = "swap_point_accumulate")]
    public decimal SwapPointAccumulate { get; private set; }

    [JsonProperty(PropertyName = "require_collateral")]
    public decimal RequireCollateral { get; private set; }

    [JsonProperty(PropertyName = "open_date")]
    public DateTime OpenDate { get; private set; }

    [JsonProperty(PropertyName = "leverage")]
    public decimal Leverage { get; private set; }

    [JsonProperty(PropertyName = "pnl")]
    public decimal ProfitAndLoss { get; private set; }

    [JsonProperty(PropertyName = "sfd")]
    public decimal SwapForDifference { get; private set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// Get Open Interest Summary
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetPositions">Online help</see>
    /// </summary>
    /// <param name="productCode"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfPosition[]>> GetPositionsAsync(string productCode, CancellationToken ct)
    {
        return GetPrivateAsync<BfPosition[]>(nameof(GetPositionsAsync), "product_code=" + productCode, ct);
    }

    public async Task<BfPosition[]> GetPositionsAsync(string productCode) => (await GetPositionsAsync(productCode, CancellationToken.None)).GetContent();
}
