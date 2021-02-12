//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;

namespace BitFlyerDotNet.Trading
{
    public interface IBfxOrderTransaction
    {
        Guid Id { get; }
        DateTime OpenTime { get; }
        BfxOrderTransactionState State { get; }
        IBfxOrder Order { get; }

        bool IsCancelable { get; }
        void Cancel();
        bool HasParent { get; }
    }
}
