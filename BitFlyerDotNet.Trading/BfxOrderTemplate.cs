//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

public enum PriceType
{
    Price,
    LTP,
    Ask,
    Bid,
    Mid,
}

public class BfxOrderTemplate
{
    public Ulid Id { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public BfOrderType OrderType { get; set; }
    public BfTradeSide Side { get; set; }
    public PriceType TriggerPriceType { get; set; }
    public string TriggerPriceOffset { get; set; }
    public PriceType OrderPriceType { get; set; }
    public string OrderPriceOffset { get; set; }

    public class BfxChildOrderTemplate
    {
        public BfOrderType OrderType { get; set; }
        public BfTradeSide Side { get; set; }
        public PriceType TriggerPriceType { get; set; }
        public string TriggerPriceOffset { get; set; }
        public PriceType OrderPriceType { get; set; }
        public string OrderPriceOffset { get; set; }
    }
    public BfxChildOrderTemplate[] Children { get; set; }

    public IBfOrder CreateOrder(string productCode, decimal size, BfTicker ticker)
    {
        switch (OrderType)
        {
            case BfOrderType.Market:
            case BfOrderType.Limit:
                return CreateChildOrder(productCode, size, ticker);

            default:
                return CreateParentOrder(productCode, size, ticker);
        }
    }

    BfChildOrder CreateChildOrder(string productCode, decimal size, BfTicker ticker)
    {
        var order = new BfChildOrder();
        order.ProductCode = productCode;
        order.Size = size;
        order.ChildOrderType = OrderType;
        order.Side = Side;

        if (order.ChildOrderType == BfOrderType.Limit)
        {
            order.Price = CalcPrice(OrderPriceType, OrderPriceOffset, ticker);
        }

        return order;
    }

    BfParentOrder CreateParentOrder(string productCode, decimal size, BfTicker ticker)
    {
        var order = new BfParentOrder();
        return order;
    }

    decimal CalcPrice(PriceType priceType, string priceOffset, BfTicker ticker)
    {
        decimal price;
        switch (priceType)
        {
            case PriceType.LTP:
                price = ticker.LastTradedPrice;
                break;

            case PriceType.Ask:
                price = ticker.BestAsk;
                break;

            case PriceType.Bid:
                price = ticker.BestBid;
                break;

            default:
                throw new Exception($"Illegal price type {priceType}");
        }

        decimal offset;
        priceOffset = priceOffset.ToUpper();
        if (priceOffset.EndsWith("%"))
        {
            offset = decimal.Parse(priceOffset.Replace("%", "")) * 0.01m * price;
        }
        else if (priceOffset.EndsWith("BP")) // basis point
        {
            offset = decimal.Parse(priceOffset.Replace("BP", "")) * 0.0001m * price;
        }
        else
        {
            offset = decimal.Parse(priceOffset);
        }
        price += offset;

        return price;
    }
}

public class BfxOrderTemplateManager
{
    public BfxOrderTemplate[] Templates { get; private set; }

    public static BfxOrderTemplateManager Load(string jsonFilePath)
    {
        var tempMan = new BfxOrderTemplateManager();
        tempMan.Templates = JsonConvert.DeserializeObject<BfxOrderTemplate[]>(File.ReadAllText(jsonFilePath));

        return tempMan;
    }

    BfxOrderTemplateManager()
    {
    }
}

public static partial class BfxAppliationExtension
{
    //public static BfxApplication AddOrderTemplates()
}