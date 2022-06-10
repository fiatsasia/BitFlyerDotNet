//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfCancelAllChildOrdersRequest
{
    public string ProductCode { get; set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// Cancel All Orders
    /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelAllChildOrdersAsync">Online help</see>
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<string>> CancelAllChildOrdersAsync(string productCode, CancellationToken ct)
        => PostPrivateAsync<string>(nameof(CancelAllChildOrdersAsync), new BfCancelAllChildOrdersRequest { ProductCode = productCode }, ct);

    /// <summary>
    /// Cancel All Orders
    /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelAllChildOrdersAsync">Online help</see>
    /// </summary>
    /// <param name="productCode"></param>
    /// <returns></returns>
    public async Task<bool> CancelAllChildOrdersAsync(string productCode) => (await CancelAllChildOrdersAsync(productCode, CancellationToken.None)).IsOk;
}
