//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet
{
    public class BfChildOrder : IBfChildOrder
    {
        public string ProductCode { get; }
        public BfOrderType OrderType { get; }
        public BfTradeSide Side { get; }
        public decimal? OrderPrice { get; }
        public decimal OrderSize { get; }
        public DateTime? OrderDate { get; }
        public DateTime? ExpireDate { get; }
        public BfOrderState State { get; }
        public string AcceptanceId { get; }
        public string OrderId { get; }

        public IBfPrivateExecution[] Executions { get; set; }

        public BfChildOrder(BfaChildOrder order, BfaPrivateExecution[] execs)
        {
            ProductCode = order.ProductCode;
            OrderType = order.ChildOrderType;
            Side = order.Side;
            if (OrderType == BfOrderType.Limit)
            {
                OrderPrice = order.Price;
            }
            OrderSize = order.Size;
            OrderDate = order.ChildOrderDate;
            ExpireDate = order.ExpireDate;
            State = order.ChildOrderState;
            AcceptanceId = order.ChildOrderAcceptanceId;
            OrderId = order.ChildOrderId;

            Executions = execs.Select(e => new BfExecution(e)).Cast<IBfPrivateExecution>().ToArray();
        }
    }
}
