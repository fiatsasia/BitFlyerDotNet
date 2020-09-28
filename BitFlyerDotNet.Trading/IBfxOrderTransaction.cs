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
        BfxOrderTransactionState State { get; }
        IBfxOrder Order { get; }

        event EventHandler<BfxOrderTransactionEventArgs> OrderTransactionEvent;

        bool IsCancelable { get; }
        void Cancel();
        bool HasParent { get; }
    }
}
