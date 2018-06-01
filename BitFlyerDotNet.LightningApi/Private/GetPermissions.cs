//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
