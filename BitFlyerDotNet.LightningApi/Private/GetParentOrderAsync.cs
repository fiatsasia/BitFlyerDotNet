﻿//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfParentOrderDetailStatusParameter
{
    [JsonProperty(PropertyName = "product_code")]
    public string ProductCode { get; private set; }

    [JsonProperty(PropertyName = "condition_type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public BfOrderType ConditionType { get; private set; }

    [JsonProperty(PropertyName = "side")]
    [JsonConverter(typeof(StringEnumConverter))]
    public BfTradeSide Side { get; private set; }

    [JsonProperty(PropertyName = "price")]
    public decimal Price { get; private set; }         // value is 0 when not necessarily

    [JsonProperty(PropertyName = "size")]
    public decimal Size { get; private set; }

    [JsonProperty(PropertyName = "trigger_price")]
    public decimal TriggerPrice { get; private set; }  // value is 0 when not necessarily

    [JsonProperty(PropertyName = "offset")]
    public decimal Offset { get; private set; }        // value is 0 when not necessarily
}

public class BfParentOrderDetailStatus
{
    [JsonProperty(PropertyName = "id")]
    public uint PagingId { get; private set; }

    [JsonProperty(PropertyName = "parent_order_id")]
    public string ParentOrderId { get; private set; }

    [JsonProperty(PropertyName = "order_method")]
    [JsonConverter(typeof(StringEnumConverter))]
    public BfOrderType OrderMethod { get; private set; }

    [JsonProperty(PropertyName = "expire_date")]
    public DateTime ExpireDate { get; private set; }

    [JsonProperty(PropertyName = "time_in_force")]
    [JsonConverter(typeof(StringEnumConverter))]
    public BfTimeInForce TimeInForce { get; set; }

    [JsonProperty(PropertyName = "parameters")]
    public BfParentOrderDetailStatusParameter[] Parameters { get; private set; }

    [JsonProperty(PropertyName = "parent_order_acceptance_id")]
    public string ParentOrderAcceptanceId { get; private set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// Get Parent Order Details
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetParentOrder">Online help</see>
    /// </summary>
    /// <param name="productCode"></param>
    /// <param name="parentOrderId"></param>
    /// <param name="parentOrderAcceptanceId"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfParentOrderDetailStatus>> GetParentOrderAsync(string productCode, string parentOrderId, string parentOrderAcceptanceId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(parentOrderId) && string.IsNullOrEmpty(parentOrderAcceptanceId))
        {
            throw new ArgumentException();
        }

        var query = string.Format("product_code={0}{1}{2}",
            productCode,
            !string.IsNullOrEmpty(parentOrderId) ? "&parent_order_id=" + parentOrderId : "",
            !string.IsNullOrEmpty(parentOrderAcceptanceId) ? "&parent_order_acceptance_id=" + parentOrderAcceptanceId : ""
        );

        return GetPrivateAsync<BfParentOrderDetailStatus>(nameof(GetParentOrderAsync), query, ct);
    }

    public async Task<BfParentOrderDetailStatus> GetParentOrderAsync(string productCode, string parentOrderId = null, string parentOrderAcceptanceId = null)
        => (await GetParentOrderAsync(productCode, parentOrderId, parentOrderAcceptanceId, CancellationToken.None)).Deserialize();
}
