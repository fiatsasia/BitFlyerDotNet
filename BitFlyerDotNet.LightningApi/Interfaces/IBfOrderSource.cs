//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
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
