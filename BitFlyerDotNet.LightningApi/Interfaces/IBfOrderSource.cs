//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System.Collections.Generic;

namespace BitFlyerDotNet.LightningApi
{
    public interface IBfOrderSource
    {
        void UpdateActiveOrders();
        IEnumerable<IBfParentOrder> GetActiveParentOrders();
        IEnumerable<IBfChildOrder> GetActiveIndependentChildOrders();
        IBfParentOrder GetParentOrder(string parentOrderAcceptanceId);
        IBfChildOrder GetChildOrder(string childOrderAcceptanceId);

        // Parent orders
        void OpenParentOrder(BfParentOrderRequest req, BfParentOrderResponse resp);
        void RegisterParentOrderEvent(BfParentOrderEvent poe);

        // Child orders
        void OpenChildOrder(BfChildOrderRequest req, BfChildOrderResponse resp);
        void RegisterChildOrderEvent(BfChildOrderEvent coe);
    }
}
