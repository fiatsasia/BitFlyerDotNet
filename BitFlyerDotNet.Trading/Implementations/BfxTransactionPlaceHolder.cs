//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Collections.Generic;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    class BfxTransactionPlaceHolder : IBfxTransaction
    {
        public Guid Id => throw new NotImplementedException();
        public DateTime OpenTime => throw new NotImplementedException();
        public BfxTransactionState State => throw new NotImplementedException();
        public bool IsCancelable => throw new NotImplementedException();
        public bool HasParent => throw new NotImplementedException();
        public IBfxOrder Order => throw new NotImplementedException();

        public void Cancel()
        {
            throw new NotImplementedException();
        }
    }

    class BfxParentTransactionPlaceHolder : BfxTransactionPlaceHolder
    {
        public List<BfParentOrderEvent> ParentOrderEvents { get; } = new List<BfParentOrderEvent>();

        public BfxParentTransactionPlaceHolder()
        {
        }
    }

    class BfxChildTransactionPlaceHolder : BfxTransactionPlaceHolder
    {
        public List<BfChildOrderEvent> ChildOrderEvents { get; } = new List<BfChildOrderEvent>();

        public BfxChildTransactionPlaceHolder()
        {
        }
    }
}
