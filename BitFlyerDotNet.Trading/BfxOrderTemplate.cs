//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

#pragma warning disable CS8602
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

public class BfxOrderTemplateBase
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

public class BfxOrderTemplate : BfxOrderTemplateBase
{
    public Ulid Id { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public BfxOrderTemplateBase[] Children { get; set; }

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
        decimal? price = default;
        if (OrderType == BfOrderType.Limit)
        {
            price = GetTickerPrice(OrderPriceType, ticker);
            if (!string.IsNullOrEmpty(OrderPriceOffset))
            {
                price += BfProductCode.RoundPrice(productCode, price.Value * ParseOffset(OrderPriceOffset));
            }
        }

        int? minuteToExpire = default;
        if (ExpirationPeriod.HasValue)
        {
            minuteToExpire = (int)Math.Round(ExpirationPeriod.Value.TotalMinutes, 0);
        }

        return new()
        {
            ProductCode = productCode,
            Size = size,
            Price = price,
            ChildOrderType = OrderType,
            Side = Side,
            MinuteToExpire = minuteToExpire,
            TimeInForce = TimeInForce,
        };
    }

    BfParentOrderParameter CreateParameter(BfxOrderTemplateBase templ, string productCode, decimal size, BfTicker ticker)
    {
        decimal? price = default;
        if (templ.OrderType == BfOrderType.Limit || templ.OrderType == BfOrderType.StopLimit)
        {
            price = GetTickerPrice(templ.OrderPriceType, ticker);
            if (!string.IsNullOrEmpty(templ.OrderPriceOffset))
            {
                price += BfProductCode.RoundPrice(productCode, price.Value * ParseOffset(templ.OrderPriceOffset));
            }
        }

        decimal? triggerPrice = default;
        if (templ.OrderType == BfOrderType.Stop || templ.OrderType == BfOrderType.StopLimit)
        {
            triggerPrice = GetTickerPrice(templ.TriggerPriceType, ticker);
            if (!string.IsNullOrEmpty(templ.TriggerPriceOffset))
            {
                triggerPrice += BfProductCode.RoundPrice(productCode, triggerPrice.Value * ParseOffset(templ.TriggerPriceOffset));
            }
        }

        decimal? offset = default;
        if (templ.OrderType == BfOrderType.Trail)
        {
            offset = GetTickerPrice(templ.TrailOffsetType, ticker);
            if (!string.IsNullOrEmpty(templ.TrailOffsetRatio))
            {
                offset = BfProductCode.RoundPrice(productCode, offset.Value * ParseOffset(templ.TrailOffsetRatio));
            }
        }

        return new()
        {
            ProductCode = productCode,
            ConditionType = templ.OrderType,
            Side = templ.Side,
            Size = size,
            Price = price,
            TriggerPrice = triggerPrice,
            Offset = offset,
        };
    }

    BfParentOrder CreateParentOrder(string productCode, decimal size, BfTicker ticker)
    {
        var order = new BfParentOrder
        {
            OrderMethod = OrderType.IsSimpleConditionType() ? BfOrderType.Simple : OrderType,
            TimeInForce = TimeInForce,
            MinuteToExpire = ExpirationPeriod.HasValue ? (int)Math.Round(ExpirationPeriod.Value.TotalMinutes, 0) : default,
        };

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