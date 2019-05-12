//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

namespace BitFlyerDotNet.LightningApi
{
    public partial class BitFlyerClient
    {
        public BitFlyerResponse<string[]> GetPermissions()
        {
            return PrivateGet<string[]>(nameof(GetPermissions));
        }
    }
}
