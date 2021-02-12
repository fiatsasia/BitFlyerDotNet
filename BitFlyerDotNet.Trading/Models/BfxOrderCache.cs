//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System.Linq;
using System.Collections.Generic;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxOrderCache : IBfOrderSource
    {
        readonly BitFlyerClient _client;
        readonly BfProductCode _productCode;

        public BfxOrderCache(BitFlyerClient client, BfProductCode productCode)
        {
            _client = client;
            _productCode = productCode;
        }

        IEnumerable<IBfChildOrder> IBfOrderSource.GetActiveIndependentChildOrders()
        {
            var activeDescendants = _client.GetParentOrders(_productCode, orderState: BfOrderState.Active).GetContent()
                .Select(parent => _client.GetChildOrders(_productCode, parentOrderId: parent.ParentOrderId).GetContent())
                .SelectMany(e => e);
            var activeChildren = _client.GetChildOrders(_productCode, orderState: BfOrderState.Active).GetContent();

            return activeChildren.Where(e => !activeDescendants.Any(f => e.ChildOrderAcceptanceId == f.ChildOrderAcceptanceId))
                .Select(e => new BfChildOrder(e, _client.GetPrivateExecutions(_productCode, childOrderAcceptanceId: e.ChildOrderAcceptanceId).GetContent()));
        }

        IEnumerable<IBfParentOrder> IBfOrderSource.GetActiveParentOrders()
        {
            return _client.GetParentOrders(_productCode, orderState: BfOrderState.Active).GetContent().Select(parent => new BfParentOrder(parent,
                _client.GetParentOrderDetail(_productCode, parentOrderAcceptanceId: parent.ParentOrderAcceptanceId).GetContent(),
                _client.GetChildOrders(_productCode, parentOrderId: parent.ParentOrderId).GetContent().Select(child => new BfChildOrder(child,
                    _client.GetPrivateExecutions(_productCode, childOrderAcceptanceId: child.ChildOrderAcceptanceId).GetContent()
                )).Cast<IBfChildOrder>().ToArray()
            ));
        }

        public IBfChildOrder GetChildOrder(string childOrderAcceptanceId)
        {
            var order = _client.GetChildOrders(_productCode, childOrderAcceptanceId: childOrderAcceptanceId).GetContent()[0];
            var execs = _client.GetPrivateExecutions(_productCode, childOrderAcceptanceId: childOrderAcceptanceId).GetContent();
            return new BfChildOrder(order, execs);
        }

        IBfParentOrder IBfOrderSource.GetParentOrder(string parentOrderAcceptanceId)
        {
            var detail = _client.GetParentOrderDetail(_productCode, parentOrderAcceptanceId: parentOrderAcceptanceId).GetContent();
            var parent = _client.GetParentOrder(_productCode, detail);
            return new BfParentOrder(parent, detail,
                _client.GetChildOrders(_productCode, parentOrderId: parent.ParentOrderId).GetContent().Select(child => new BfChildOrder(child,
                    _client.GetPrivateExecutions(_productCode, childOrderAcceptanceId: child.ChildOrderAcceptanceId).GetContent()
                )).Cast<IBfChildOrder>().ToArray()
            );
        }

        void IBfOrderSource.OpenChildOrder(BfChildOrderRequest req, BfChildOrderResponse resp) { }
        void IBfOrderSource.OpenParentOrder(BfParentOrderRequest req, BfParentOrderResponse resp) { }
        void IBfOrderSource.RegisterChildOrderEvent(BfChildOrderEvent coe) { }
        void IBfOrderSource.RegisterParentOrderEvent(BfParentOrderEvent poe) { }
        void IBfOrderSource.UpdateActiveOrders() { }
    }
}
