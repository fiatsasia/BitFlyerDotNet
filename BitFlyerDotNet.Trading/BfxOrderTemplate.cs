//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

#pragma warning disable CS8618

namespace BitFlyerDotNet.Trading;

public enum PriceType
{
    Price,
    LTP,
    Ask,
    Bid,
    Mid,
}

public class BfxChildOrderTemplateBase
{
    public BfOrderType OrderType { get; set; }
    public BfTradeSide Side { get; set; }
    public PriceType TriggerPriceType { get; set; }
    public string TriggerPriceOffset { get; set; }
    public PriceType OrderPriceType { get; set; }
    public string OrderPriceOffset { get; set; }
    public PriceType TrailOffsetType { get; set; }
    public string TrailOffsetRatio { get; set; }
    public TimeSpan? ExpirationPeriod { get; set; }
    public BfTimeInForce? TimeInForce { get; set; }
}

public class BfxOrderTemplate : BfxChildOrderTemplateBase
{
    public Ulid Id { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public BfxChildOrderTemplateBase[] Children { get; set; }

    internal BfxApplication App { get; set; }

    public async Task<IBfOrder> CreateOrderAsync(string productCode, decimal size)
    {
        var ticker = (await App.GetMarketDataSourceAsync(productCode)).Ticker;
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
            order.Price = GetTickerPrice(OrderPriceType, ticker);
            if (!string.IsNullOrEmpty(OrderPriceOffset))
            {
                order.Price += BfProductCode.RoundPrice(productCode, order.Price.Value * ParseOffset(OrderPriceOffset));
            }
        }

        if (ExpirationPeriod.HasValue)
        {
            order.MinuteToExpire = (int)Math.Round(ExpirationPeriod.Value.TotalMinutes, 0);
        }
        order.TimeInForce = TimeInForce;

        return order;
    }

    BfParentOrderParameter CreateParameter(BfxChildOrderTemplateBase templ, string productCode, decimal size, BfTicker ticker)
    {
        var order = new BfParentOrderParameter
        {
            ProductCode = productCode,
            ConditionType = templ.OrderType,
            Side = templ.Side,
            Size = size
        };

        if (templ.OrderType == BfOrderType.Limit || templ.OrderType == BfOrderType.StopLimit)
        {
            order.Price = GetTickerPrice(templ.OrderPriceType, ticker);
            if (!string.IsNullOrEmpty(templ.OrderPriceOffset))
            {
                order.Price += BfProductCode.RoundPrice(productCode, order.Price.Value * ParseOffset(templ.OrderPriceOffset));
            }
        }

        if (templ.OrderType == BfOrderType.Stop || templ.OrderType == BfOrderType.StopLimit)
        {
            order.TriggerPrice = GetTickerPrice(templ.TriggerPriceType, ticker);
            if (!string.IsNullOrEmpty(templ.TriggerPriceOffset))
            {
                order.TriggerPrice += BfProductCode.RoundPrice(productCode, order.TriggerPrice.Value * ParseOffset(templ.TriggerPriceOffset));
            }
        }

        if (templ.OrderType == BfOrderType.Trail)
        {
            order.Offset = GetTickerPrice(templ.TrailOffsetType, ticker);
            if (!string.IsNullOrEmpty(templ.TrailOffsetRatio))
            {
                order.Offset = BfProductCode.RoundPrice(productCode, order.Offset.Value * ParseOffset(templ.TrailOffsetRatio));
            }
        }

        return order;
    }

    BfParentOrder CreateParentOrder(string productCode, decimal size, BfTicker ticker)
    {
        var order = new BfParentOrder();
        order.OrderMethod = OrderType.IsSimpleConditionType() ? BfOrderType.Simple : OrderType;
        if (order.OrderMethod == BfOrderType.Simple)
        {
            order.Parameters.Add(CreateParameter(this, productCode, size, ticker));
        }
        else
        {
            foreach (var child in Children)
            {
                order.Parameters.Add(CreateParameter(child, productCode, size, ticker));
            }
        }

        if (ExpirationPeriod.HasValue)
        {
            order.MinuteToExpire = (int)Math.Round(ExpirationPeriod.Value.TotalMinutes, 0);
        }
        order.TimeInForce = TimeInForce;

        return order;
    }

    decimal GetTickerPrice(PriceType priceType, BfTicker ticker)
        => priceType switch { PriceType.LTP => ticker.LastTradedPrice, PriceType.Ask => ticker.BestAsk, PriceType.Bid => ticker.BestBid, _ => throw new Exception($"Illegal price type: '{priceType}'") };

    decimal ParseOffset(string priceOffset)
    {
        decimal offset;
        priceOffset = priceOffset.ToUpper();
        if (priceOffset.EndsWith("%"))
        {
            offset = decimal.Parse(priceOffset.Replace("%", "")) * 0.01m;
        }
        else if (priceOffset.EndsWith("BP")) // basis point
        {
            offset = decimal.Parse(priceOffset.Replace("BP", "")) * 0.0001m;
        }
        else
        {
            offset = decimal.Parse(priceOffset);
        }

        return offset;
    }
}

public static class BfxPrderTemplateManagerExtension
{
    static Dictionary<Ulid, BfxOrderTemplate[]> _templates = new();

    public static BfxApplication AddOrderTemplates(this BfxApplication app, string jsonFilePath)
    {
        if (_templates.ContainsKey(app.Id))
        {
            return app;
        }

        var templates = JsonConvert.DeserializeObject<BfxOrderTemplate[]>(File.ReadAllText(jsonFilePath));
        foreach (var templ in templates)
        {
            templ.App = app;
        }
        _templates.Add(app.Id, templates);

        return app;
    }

    public static BfxOrderTemplate[] GetOrderTemplates(this BfxApplication app)
    {
        return _templates[app.Id];
    }

    public static BfxOrderTemplate GetOrderTemplateByDescription(this BfxApplication app, string description)
    {
        return _templates[app.Id].First(e => e.Description == description);
    }
}