//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

public class BfxOrderChangedEventArgs : EventArgs
{
    #region bitFlyer Lightning "order pane" properties
    public DateTime? OrderDate { get; }
    public string ProductCode { get; }
    public BfOrderType OrderType { get; }
    public BfTradeSide? Side { get; }
    public decimal? OrderSize { get; }
    public decimal? OutstandingSize { get; }     // Order size - executed size
    public decimal? OrderPrice { get; }
    public decimal AveragePrice { get; }
    #endregion bitFlyer Lightning "order pane" properties

    public decimal? TriggerPrice { get; set; }
    public decimal? TrailOffset { get; set; }

    public DateTime EventDate { get; }
    public BfOrderEventType EventType { get; }
    public string? OrderFailedReason { get; }        // Order/Cancel failed reason
    public decimal Commission { get; }          // Total commission in order
    public decimal SwapForDifference { get; }   // Total SFD in order

    public BfxOrderChangedEventArgs(BfChildOrderEvent e, BfxTrade trade)
    {
        OrderDate = trade.OrderDate;
        ProductCode = trade.ProductCode;
        OrderType = trade.OrderType;
        Side = trade.Side;
        OrderSize = trade.OrderSize;
        OutstandingSize = trade.OutstandingSize;
        OrderPrice = trade.OrderPrice;
        TriggerPrice = trade.TriggerPrice;
        TrailOffset = trade.TrailOffset;
        EventDate = e.EventDate;
        EventType = e.EventType;
        OrderFailedReason = e.OrderFailedReason;
    }

    public BfxOrderChangedEventArgs(BfParentOrderEvent e, BfxTrade trade)
    {
    }
}
