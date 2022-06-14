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
    public string ProductCode { get; private set; }
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
    public ReadOnlyCollection<BfxOrder> Children => Array.AsReadOnly(_children);

    BfxOrder[] _children;

    public BfxOrder(BfxTrade trade)
    {
        ProductCode = trade.ProductCode;

        switch (trade.OrderType)
        {
            case BfOrderType.Market:
            case BfOrderType.Limit:
            case BfOrderType.Stop:
            case BfOrderType.StopLimit:
            case BfOrderType.Trail:
                SetCommonPart(trade);
                SetIndividualPart(trade);
                break;

            case BfOrderType.Simple:
                SetCommonPart(trade);
                SetIndividualPart(trade.Children[0]);
                OrderType = trade.Children[0].OrderType;
                break;

            case BfOrderType.IFD:
            case BfOrderType.OCO:
            case BfOrderType.IFDOCO:
                SetCommonPart(trade);
                break;
        }

        _children = new BfxOrder[trade.OrderType.GetChildCount()];
        for (int i = 0; i < _children.Length; i++)
        {
            _children[i] = new(trade.Children[i]);
        }
    }

    void SetCommonPart(BfxTrade trade)
    {
        OrderType = trade.OrderType;
        OrderAcceptanceId = trade.OrderAcceptanceId;
        OrderId = trade.OrderId;
        OrderDate = trade.OrderDate;
        ExpireDate = trade.ExpireDate;
        OrderState = trade.OrderState;
    }

    void SetIndividualPart(BfxTrade trade)
    {
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
                Side = trade.Side;
                OrderSize = trade.OrderSize;
                TriggerPrice = trade.TriggerPrice;
                break;

            case BfOrderType.StopLimit:
                Side = trade.Side;
                OrderSize = trade.OrderSize;
                OrderPrice = trade.OrderPrice;
                TriggerPrice = trade.TriggerPrice;
                break;

            case BfOrderType.Trail:
                Side = trade.Side;
                OrderSize = trade.OrderSize;
                TrailOffset = trade.TrailOffset;
                break;

            case BfOrderType.IFD:
            case BfOrderType.OCO:
            case BfOrderType.IFDOCO:
                throw new ArgumentException();
        }
    }
}
