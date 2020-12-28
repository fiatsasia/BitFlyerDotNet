//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi
{
    public partial class BitFlyerClient
    {
        /// <summary>
        /// Get API Key Permissions
        /// <see href="https://scrapbox.io/BitFlyerDotNet/GetPermissions">Online help</see>
        /// </summary>
        /// <returns></returns>
        public BitFlyerResponse<string[]> GetPermissions()
        {
            return GetPrivateAsync<string[]>(nameof(GetPermissions)).Result;
        }
    }
}
