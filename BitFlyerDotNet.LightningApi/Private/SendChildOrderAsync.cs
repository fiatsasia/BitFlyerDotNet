//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfChildOrder : IBfOrder
{
    public string ProductCode { get; init; }

    [JsonConverter(typeof(StringEnumConverter))]
    public BfOrderType ChildOrderType { get; init; }

    [JsonConverter(typeof(StringEnumConverter))]
    public BfTradeSide Side { get; init; }

    [JsonConverter(typeof(DecimalJsonConverter))]
    public decimal? Price { get; init; }
    public bool ShouldSerializePrice() => Price.HasValue;

    [JsonConverter(typeof(DecimalJsonConverter))]
    public decimal Size { get; init; }

    public int? MinuteToExpire { get; init; }
    public bool ShouldSerializeMinuteToExpire() => (MinuteToExpire.HasValue && MinuteToExpire.Value > 0); // default = 43200 (30 days)

    [JsonConverter(typeof(StringEnumConverter))]
    public BfTimeInForce? TimeInForce { get; init; }
    public bool ShouldSerializeTimeInForce() => TimeInForce.HasValue;

    // This will be used order factory
    public static implicit operator BfParentOrderParameter(BfChildOrder order)
    {
        return new()
        {
            ProductCode = order.ProductCode,
            ConditionType = order.ChildOrderType,
            Side = order.Side,
            Price = order.Price,
            Size = order.Size
        };
    }
}

public class BfChildOrderAcceptance
{
    [JsonProperty(PropertyName = "child_order_acceptance_id")]
    public string ChildOrderAcceptanceId { get; private set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// Send a New Order
    /// <see href="https://scrapbox.io/BitFlyerDotNet/SendChildOrder">Online help</see>
    /// </summary>
    /// <param name="order"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfChildOrderAcceptance>> SendChildOrderAsync(BfChildOrder order, CancellationToken ct)
    {
        return PostPrivateAsync<BfChildOrderAcceptance>(nameof(SendChildOrderAsync), order, ct);
    }

    /// <summary>
    /// Send a New Order
    /// <see href="https://scrapbox.io/BitFlyerDotNet/SendChildOrder">Online help</see>
    /// </summary>
    /// <param name="order"></param>
    /// <returns></returns>
    public async Task<BfChildOrderAcceptance> SendChildOrderAsync(BfChildOrder order)
    {
        return (await SendChildOrderAsync(order, CancellationToken.None)).Deserialize();
    }
}
