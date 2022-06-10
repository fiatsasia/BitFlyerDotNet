//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public partial class BitFlyerClient
{
    /// <summary>
    /// Get API Key Permissions
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetPermissions">Online help</see>
    /// </summary>
    /// <returns></returns>
    public Task<BitFlyerResponse<string[]>> GetPermissionsAsync(CancellationToken ct)
        => GetPrivateAsync<string[]>(nameof(GetPermissionsAsync), string.Empty, ct);

    public async Task<string[]> GetPermissionsAsync() => (await GetPermissionsAsync(CancellationToken.None)).GetContent();
}
