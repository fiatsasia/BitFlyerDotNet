﻿//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfParentOrderParameter
{
    public string ProductCode { get; init; }

    [JsonConverter(typeof(StringEnumConverter))]
    public BfOrderType ConditionType { get; init; }

    [JsonConverter(typeof(StringEnumConverter))]
    public BfTradeSide Side { get; init; }

    [JsonConverter(typeof(DecimalJsonConverter))]
    public decimal? Price { get; init; }
    public bool ShouldSerializePrice() { return ConditionType == BfOrderType.Limit || ConditionType == BfOrderType.StopLimit; }

    [JsonConverter(typeof(DecimalJsonConverter))]
    public decimal Size { get; init; }

    [JsonConverter(typeof(DecimalJsonConverter))]
    public decimal? TriggerPrice { get; init; }
    public bool ShouldSerializeTriggerPrice() { return ConditionType == BfOrderType.Stop || ConditionType == BfOrderType.StopLimit; }

    [JsonConverter(typeof(DecimalJsonConverter))]
    public decimal? Offset { get; init; }
    public bool ShouldSerializeOffset() { return ConditionType == BfOrderType.Trail; }
}

public class BfParentOrder : IBfOrder
{
    [JsonConverter(typeof(StringEnumConverter))]
    public BfOrderType OrderMethod { get; init; }

    public int? MinuteToExpire { get; init; }
    public bool ShouldSerializeMinuteToExpire() => (MinuteToExpire.HasValue && MinuteToExpire.Value > 0); // default = 43200 (30 days)

    [JsonConverter(typeof(StringEnumConverter))]
    public BfTimeInForce? TimeInForce { get; init; }
    public bool ShouldSerializeTimeInForce() => TimeInForce.HasValue;

    public List<BfParentOrderParameter> Parameters { get; init; } = new ();


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

public class BfParentOrderAcceptance
{
    [JsonProperty(PropertyName = "parent_order_acceptance_id")]
    public string ParentOrderAcceptanceId { get; private set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// Submit New Parent Order (Special order)
    /// <see href="https://scrapbox.io/BitFlyerDotNet/SendParentOrder">Online help</see>
    /// </summary>
    /// <param name="order"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfParentOrderAcceptance>> SendParentOrderAsync(BfParentOrder order, CancellationToken ct)
    {
        return PostPrivateAsync<BfParentOrderAcceptance>(nameof(SendParentOrderAsync), order, ct);
    }

    /// <summary>
    /// Submit New Parent Order (Special order)
    /// <see href="https://scrapbox.io/BitFlyerDotNet/SendParentOrder">Online help</see>
    /// </summary>
    /// <param name="order"></param>
    /// <returns></returns>
    public async Task<BfParentOrderAcceptance> SendParentOrderAsync(BfParentOrder order)
        => (await SendParentOrderAsync(order, CancellationToken.None)).Deserialize();
}
