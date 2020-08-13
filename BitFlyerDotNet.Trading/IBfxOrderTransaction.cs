//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;

namespace BitFlyerDotNet.Trading
{
    public interface IBfxOrderTransaction
    {
        Guid Id { get; }
        DateTime OpenTime { get; }

        bool IsCancelable { get; }
        void Cancel();
    }

    internal interface IBfxParentOrderTransaction
    {
    }

    internal interface IBfxChildOrderTransaction
    {
    }
}
