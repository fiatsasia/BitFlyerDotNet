//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
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
            return PrivateGet<string[]>(nameof(GetPermissions));
        }
    }
}
