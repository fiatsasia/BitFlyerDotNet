//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

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
    public DateTime? OrderDate { get; private set; }
    public DateTime? ExpireDate { get; private set; }
    public BfOrderState? OrderState { get; private set; }

    public BfxOrder(BfxTrade trade)
    {
        OrderType = trade.OrderType;
        OrderAcceptanceId = trade.OrderAcceptanceId;
        OrderId = trade.OrderId;
        OrderDate = trade.OrderDate;
        ExpireDate = trade.ExpireDate;
        OrderState = trade.OrderState;

        switch (OrderType)
        {
            case BfOrderType.Market:
                Side = trade.Side;
                OrderSize = trade.OrderSize;
                break;

            case BfOrderType.Limit:
                Side = trade.Side;
                OrderSize = trade.OrderSize;
                OrderPrice = trade.OrderPrice;
                break;

            case BfOrderType.Stop:
                Side = trade.Children[0].Side;
                OrderSize = trade.Children[0].OrderSize;
                TriggerPrice = trade.Children[0].TriggerPrice;
                break;

            case BfOrderType.StopLimit:
                Side = trade.Children[0].Side;
                OrderSize = trade.Children[0].OrderSize;
                OrderPrice = trade.Children[0].OrderPrice;
                TriggerPrice = trade.Children[0].TriggerPrice;
                break;

            case BfOrderType.Trail:
                Side = trade.Children[0].Side;
                OrderSize = trade.Children[0].OrderSize;
                TrailOffset = trade.Children[0].TrailOffset;
                break;

            case BfOrderType.IFD:
                break;

            case BfOrderType.OCO:
                break;

            case BfOrderType.IFDOCO:
                break;
        }
    }
}
