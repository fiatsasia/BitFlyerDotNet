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
    public decimal? ExecutedPrice { get; private set; }
    public decimal? ExecutedSize { get; private set; }
    public ReadOnlyCollection<BfxOrder> Children => Array.AsReadOnly(_children);

    BfxOrder[] _children;

    public BfxOrder(BfxOrderContext os)
    {
        ProductCode = os.ProductCode;

        switch (os.OrderType)
        {
            case BfOrderType.Market:
            case BfOrderType.Limit:
            case BfOrderType.Stop:
            case BfOrderType.StopLimit:
            case BfOrderType.Trail:
                SetCommonPart(os);
                SetIndividualPart(os);
                break;

            case BfOrderType.Simple:
                SetCommonPart(os);
                SetIndividualPart(os.Children[0]);
                OrderType = os.Children[0].OrderType;
                break;

            case BfOrderType.IFD:
            case BfOrderType.OCO:
            case BfOrderType.IFDOCO:
                SetCommonPart(os);
                break;
        }

        _children = new BfxOrder[os.OrderType.GetChildCount()];
        for (int i = 0; i < _children.Length; i++)
        {
            _children[i] = new(os.Children[i]);
        }
    }

    void SetCommonPart(BfxOrderContext os)
    {
        OrderType = os.OrderType;
        OrderAcceptanceId = os.OrderAcceptanceId;
        OrderId = os.OrderId;
        OrderDate = os.OrderDate;
        ExpireDate = os.ExpireDate;
        OrderState = os.OrderState;
    }

    void SetIndividualPart(BfxOrderContext os)
    {
        Side = os.Side;
        OrderSize = os.OrderSize;
        ExecutedPrice = os.ExecutedPrice;
        ExecutedSize = os.ExecutedSize;

        switch (OrderType)
        {
            case BfOrderType.Market:
                break;

            case BfOrderType.Limit:
                OrderPrice = os.OrderPrice;
                break;

            case BfOrderType.Stop:
                TriggerPrice = os.TriggerPrice;
                break;

            case BfOrderType.StopLimit:
                OrderPrice = os.OrderPrice;
                TriggerPrice = os.TriggerPrice;
                break;

            case BfOrderType.Trail:
                TrailOffset = os.TrailOffset;
                break;

            case BfOrderType.IFD:
            case BfOrderType.OCO:
            case BfOrderType.IFDOCO:
                throw new ArgumentException();
        }
    }
}
