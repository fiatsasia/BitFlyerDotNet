//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfCancelParentOrderRequest
{
    public string ProductCode { get; set; }

    public string ParentOrderId { get; set; }
    public bool ShouldSerializeParentOrderId() { return !string.IsNullOrEmpty(ParentOrderId); }

    public string ParentOrderAcceptanceId { get; set; }
    public bool ShouldSerializeParentOrderAcceptanceId() { return !string.IsNullOrEmpty(ParentOrderAcceptanceId); }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// Cancel parent order
    /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelParentOrderAsync">Online help</see>
    /// </summary>
    /// <param name="productCode"></param>
    /// <param name="parentOrderId"></param>
    /// <param name="parentOrderAcceptanceId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<BitFlyerResponse<string>> CancelParentOrderAsync(string productCode, string parentOrderId, string parentOrderAcceptanceId, CancellationToken ct)
    {
        var request = new BfCancelParentOrderRequest
        {
            ProductCode = productCode,
            ParentOrderId = parentOrderId,
            ParentOrderAcceptanceId = parentOrderAcceptanceId
        };
        return await PostPrivateAsync<string>(nameof(CancelParentOrderAsync), request, ct);
    }

    public async Task<bool> CancelParentOrderAsync(string productCode, string parentOrderId = null, string parentOrderAcceptanceId = null)
        => (await CancelParentOrderAsync(productCode, parentOrderId, parentOrderAcceptanceId, CancellationToken.None)).IsOk;
}
