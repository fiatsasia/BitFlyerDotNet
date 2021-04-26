//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System.Threading;
using System.Threading.Tasks;

namespace BitFlyerDotNet.LightningApi
{
    public partial class BitFlyerClient
    {
        /// <summary>
        /// Get API Key Permissions
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetPermissions">Online help</see>
        /// </summary>
        /// <returns></returns>
        public Task<BitFlyerResponse<string[]>> GetPermissionsAsync(CancellationToken ct)
            => GetPrivateAsync<string[]>(nameof(GetPermissions), string.Empty, ct);

        public BitFlyerResponse<string[]> GetPermissions() => GetPermissionsAsync(CancellationToken.None).Result;
    }
}
