﻿//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    class BfxTransactionPlaceHolder : IBfxOrderTransaction
    {
        public Guid Id => throw new NotImplementedException();
        public DateTime OpenTime => throw new NotImplementedException();
        public BfxOrderTransactionState State => throw new NotImplementedException();
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
