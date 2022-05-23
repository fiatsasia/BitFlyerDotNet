//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet
{
    public class BfParentOrder : IBfParentOrder
    {
        public string ProductCode { get; }
        public BfOrderType OrderType => _order.ParentOrderType;
        public string AcceptanceId => _order.ParentOrderAcceptanceId;
        public string OrderId => _order.ParentOrderId;
        public DateTime OrderDate => _order.ParentOrderDate;
        public DateTime ExpireDate => _order.ExpireDate;
        public BfOrderState State => _order.ParentOrderState;

        public IBfChildOrder[] Children { get; }

        readonly BfaParentOrder _order;

        public BfParentOrder(BfaParentOrder order, BfaParentOrderDetail detail, IBfChildOrder[] children)
        {
            _order = order;
            ProductCode = order.ProductCode;

            Children = children;
        }
    }
}
