//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Text;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class ParentOrder : IParentOrder
    {
        public BfProductCode ProductCode { get; private set; }
        public BfOrderType OrderType { get; private set; }
        public IChildOrder[] ChildOrders { get; private set; }

        public ParentOrder(BfProductCode productCode, BfParentOrderDetail order)
        {
            ProductCode = productCode;
            OrderType = order.OrderMethod;

            var childOrders = new List<ChildOrder>();
            foreach (var childOrder in order.ChildOrders)
            {
                childOrders.Add(new ChildOrder(productCode, childOrder));
            }
            ChildOrders = childOrders.ToArray();
        }
    }
}
