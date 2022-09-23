//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfChildOrderStatus : IBfPagingElement
{
    [JsonProperty(PropertyName = "id")]
    public virtual long Id { get; set; }

    [JsonProperty(PropertyName = "child_order_id")]
    public virtual string ChildOrderId { get; set; }

    [JsonProperty(PropertyName = "product_code")]
    public virtual string ProductCode { get; set; }

    [JsonProperty(PropertyName = "side")]
    [JsonConverter(typeof(StringEnumConverter))]
    public virtual BfTradeSide Side { get; set; }

    [JsonProperty(PropertyName = "child_order_type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public virtual BfOrderType ChildOrderType { get; set; }

    [JsonProperty(PropertyName = "price")]
    public virtual decimal? Price { get; set; }

    [JsonProperty(PropertyName = "average_price")]
    public virtual decimal AveragePrice { get; set; }

    [JsonProperty(PropertyName = "size")]
    public virtual decimal Size { get; set; }

    [JsonProperty(PropertyName = "child_order_state")]
    [JsonConverter(typeof(StringEnumConverter))]
    public virtual BfOrderState ChildOrderState { get; set; }

    [JsonProperty(PropertyName = "expire_date")]
    public virtual DateTime ExpireDate { get; set; }

    [JsonProperty(PropertyName = "child_order_date")]
    public virtual DateTime ChildOrderDate { get; set; }

    [JsonProperty(PropertyName = "child_order_acceptance_id")]
    public virtual string ChildOrderAcceptanceId { get; set; }

    [JsonProperty(PropertyName = "outstanding_size")]
    public virtual decimal OutstandingSize { get; set; }

    [JsonProperty(PropertyName = "cancel_size")]
    public virtual decimal CancelSize { get; set; }

    [JsonProperty(PropertyName = "executed_size")]
    public virtual decimal ExecutedSize { get; set; }

    [JsonProperty(PropertyName = "total_commission")]
    public virtual decimal TotalCommission { get; set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// List Orders
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChildOrders">Online help</see>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="productCode"></param>
    /// <param name="orderState"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="childOrderId"></param>
    /// <param name="childOrderAcceptanceId"></param>
    /// <param name="parentOrderId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<T[]>> GetChildOrdersAsync<T>(
        string productCode,
        BfOrderState orderState,
        long count,
        long before,
        long after,
        string childOrderId,
        string childOrderAcceptanceId,
        string parentOrderId,
        CancellationToken ct
    ) where T : BfChildOrderStatus
    {
        var query = string.Format("product_code={0}{1}{2}{3}{4}{5}{6}{7}",
            productCode,
            orderState != BfOrderState.Unknown ? "&child_order_state=" + orderState.ToEnumString() : "",
            (count > 0)  ? $"&count={count}"   : "",
            (before > 0) ? $"&before={before}" : "",
            (after > 0)  ? $"&after={after}"   : "",
            !string.IsNullOrEmpty(childOrderId) ? "&child_order_id=" + childOrderId : "",
            !string.IsNullOrEmpty(childOrderAcceptanceId) ? "&child_order_acceptance_id=" + childOrderAcceptanceId : "",
            !string.IsNullOrEmpty(parentOrderId) ? "&parent_order_id=" + parentOrderId : ""
        );

        return GetPrivateAsync<T[]>(nameof(GetChildOrdersAsync), query, ct);
    }

    /// <summary>
    /// List Orders
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChildOrders">Online help</see>
    /// </summary>
    /// <param name="productCode"></param>
    /// <param name="orderState"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="childOrderId"></param>
    /// <param name="childOrderAcceptanceId"></param>
    /// <param name="parentOrderId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfChildOrderStatus[]>> GetChildOrdersAsync(
        string productCode,
        BfOrderState orderState,
        long count,
        long before,
        long after,
        string childOrderId,
        string childOrderAcceptanceId,
        string parentOrderId,
        CancellationToken ct
    ) => GetChildOrdersAsync<BfChildOrderStatus>(productCode, orderState, count, before, after, childOrderId, childOrderAcceptanceId, parentOrderId, ct);

    /// <summary>
    /// List child orders
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChildOrders">Online help</see>
    /// </summary>
    /// <param name="productCode"></param>
    /// <param name="orderState"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="childOrderId"></param>
    /// <param name="childOrderAcceptanceId"></param>
    /// <param name="parentOrderId"></param>
    /// <returns></returns>
    public async Task<T[]> GetChildOrdersAsync<T>(
        string productCode,
        BfOrderState orderState = BfOrderState.Unknown,
        long count = 0L,
        long before = 0L,
        long after = 0L,
        string childOrderId = null,
        string childOrderAcceptanceId = null,
        string parentOrderId = null
    ) where T : BfChildOrderStatus
        => (await GetChildOrdersAsync<T>(productCode, orderState, count, before, after, childOrderId, childOrderAcceptanceId, parentOrderId, CancellationToken.None)).Deserialize();

    /// <summary>
    /// List Orders
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetChildOrders">Online help</see>
    /// </summary>
    /// <param name="productCode"></param>
    /// <param name="orderState"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="childOrderId"></param>
    /// <param name="childOrderAcceptanceId"></param>
    /// <param name="parentOrderId"></param>
    /// <returns></returns>
    public async Task<BfChildOrderStatus[]> GetChildOrdersAsync(
        string productCode,
        BfOrderState orderState = BfOrderState.Unknown,
        long count = 0L,
        long before = 0L,
        long after = 0L,
        string childOrderId = null,
        string childOrderAcceptanceId = null,
        string parentOrderId = null
    ) => (await GetChildOrdersAsync<BfChildOrderStatus>(productCode, orderState, count, before, after, childOrderId, childOrderAcceptanceId, parentOrderId, CancellationToken.None)).Deserialize();
}
