//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Collections.Generic;
using System.Text;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxOrder
    {
        public BfOrderType OrderType { get; private set; }
        public BfTradeSide? Side { get; private set; }
        public decimal? OrderPrice { get; private set; }
        public decimal? OrderSize { get; private set; }
        public decimal? TriggerPrice { get; private set; }
        public decimal? TrailOffset { get; private set; }
        public string? OrderAcceptanceId { get; internal set; }
        public string? OrderId { get; private set; }

        public BfxOrder(BfxTrade trade)
        {
            OrderType = trade.OrderType;
            Side = trade.Side;
            OrderPrice = trade.OrderPrice;
            OrderSize = trade.OrderSize;
            TriggerPrice = trade.TriggerPrice;
            TrailOffset = trade.TrailOffset;
            OrderAcceptanceId = trade.OrderAcceptanceId;
            OrderId = trade.OrderId;

            if (OrderType == BfOrderType.IFD || OrderType == BfOrderType.OCO || OrderType == BfOrderType.IFDOCO)
            {
            }
        }
    }
}
