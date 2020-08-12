//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Text;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public interface IBfxOrderTransaction
    {
        void Cancel();
    }

    internal interface IBfxParentOrderTransaction
    {
    }

    internal interface IBfxChildOrderTransaction
    {
    }
}
