//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System.Collections.Generic;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    class BfxParentTransactionPlaceHolder : IBfxParentOrderTransaction
    {
        public List<BfParentOrderEvent> ParentOrderEvents { get; } = new List<BfParentOrderEvent>();

        public BfxParentTransactionPlaceHolder()
        {
        }
    }

    class BfxChildTransactionPlaceHolder : IBfxChildOrderTransaction
    {
        public List<BfChildOrderEvent> ChildOrderEvents { get; } = new List<BfChildOrderEvent>();

        public BfxChildTransactionPlaceHolder()
        {
        }
    }
}
