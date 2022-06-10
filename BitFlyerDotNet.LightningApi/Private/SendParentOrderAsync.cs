//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfParentOrderParameter
{
    public string ProductCode { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public BfOrderType ConditionType { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public BfTradeSide Side { get; set; }

    [JsonConverter(typeof(DecimalJsonConverter))]
    public decimal? Price { get; set; }
    public bool ShouldSerializePrice() { return ConditionType == BfOrderType.Limit || ConditionType == BfOrderType.StopLimit; }

    [JsonConverter(typeof(DecimalJsonConverter))]
    public decimal Size { get; set; }

    [JsonConverter(typeof(DecimalJsonConverter))]
    public decimal? TriggerPrice { get; set; }
    public bool ShouldSerializeTriggerPrice() { return ConditionType == BfOrderType.Stop || ConditionType == BfOrderType.StopLimit; }

    [JsonConverter(typeof(DecimalJsonConverter))]
    public decimal? Offset { get; set; }
    public bool ShouldSerializeOffset() { return ConditionType == BfOrderType.Trail; }
}

public class BfParentOrder
{
    [JsonConverter(typeof(StringEnumConverter))]
    public BfOrderType OrderMethod { get; set; }

    public int? MinuteToExpire { get; set; }
    public bool ShouldSerializeMinuteToExpire() => (MinuteToExpire.HasValue && MinuteToExpire.Value > 0); // default = 43200 (30 days)

    [JsonConverter(typeof(StringEnumConverter))]
    public BfTimeInForce? TimeInForce { get; set; }
    public bool ShouldSerializeTimeInForce() => (TimeInForce.HasValue && TimeInForce.Value != BfTimeInForce.NotSpecified); // default = GTC

    public List<BfParentOrderParameter> Parameters { get; set; } = new ();


    // This will be used order factory
    public static implicit operator BfParentOrderParameter(BfParentOrder order)
    {
        if (order.OrderMethod != BfOrderType.Simple)
        {
            throw new ArgumentException($"{order.OrderMethod} can not be parameter of parent.");
        }

        return order.Parameters[0];
    }
}

public class BfParentOrderResponse
{
    [JsonProperty(PropertyName = "parent_order_acceptance_id")]
    public string ParentOrderAcceptanceId { get; private set; }
}

public partial class BitFlyerClient
{
    void Validate(ref BfParentOrder request)
    {
        if (!request.OrderMethod.IsOrderMethod())
        {
            throw new ArgumentException();
        }
        foreach (var childOrder in request.Parameters)
        {
            if (!childOrder.ConditionType.IsConditionType())
            {
                throw new ArgumentException();
            }
        }

        if (request.MinuteToExpire == 0 && Config.MinuteToExpire > 0)
        {
            request.MinuteToExpire = Config.MinuteToExpire;
        }

        if (request.TimeInForce == BfTimeInForce.NotSpecified && Config.TimeInForce != BfTimeInForce.NotSpecified)
        {
            request.TimeInForce = Config.TimeInForce;
        }
    }

    /// <summary>
    /// Submit New Parent Order (Special order)
    /// <see href="https://scrapbox.io/BitFlyerDotNet/SendParentOrder">Online help</see>
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfParentOrderResponse>> SendParentOrderAsync(BfParentOrder request, CancellationToken ct)
    {
        Validate(ref request);
        return PostPrivateAsync<BfParentOrderResponse>(nameof(SendParentOrderAsync), request, ct);
    }

    /// <summary>
    /// Submit New Parent Order (Special order)
    /// <see href="https://scrapbox.io/BitFlyerDotNet/SendParentOrder">Online help</see>
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<BfParentOrderResponse> SendParentOrderAsync(BfParentOrder request)
        => (await SendParentOrderAsync(request, CancellationToken.None)).GetContent();
}
