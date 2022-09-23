//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfParentOrderStatus : IBfPagingElement
{
    [JsonProperty(PropertyName = "id")]
    public virtual long Id { get; set; }

    [JsonProperty(PropertyName = "parent_order_id")]
    public virtual string ParentOrderId { get; set; }

    [JsonProperty(PropertyName = "product_code")]
    public virtual string ProductCode { get; set; }

    [JsonProperty(PropertyName = "side")]
    [JsonConverter(typeof(StringEnumConverter))]
    public virtual BfTradeSide Side { get; set; }

    [JsonProperty(PropertyName = "parent_order_type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public virtual BfOrderType ParentOrderType { get; set; } // if request is simple, this contains children[0]

    [JsonProperty(PropertyName = "price")]
    public virtual decimal? Price { get; set; }

    [JsonProperty(PropertyName = "average_price")]
    public virtual decimal? AveragePrice { get; set; }

    [JsonProperty(PropertyName = "size")]
    public virtual decimal? Size { get; set; }

    [JsonProperty(PropertyName = "parent_order_state")]
    [JsonConverter(typeof(StringEnumConverter))]
    public virtual BfOrderState ParentOrderState { get; set; }

    [JsonProperty(PropertyName = "expire_date")]
    public virtual DateTime ExpireDate { get; set; }

    [JsonProperty(PropertyName = "parent_order_date")]
    public virtual DateTime ParentOrderDate { get; set; }

    [JsonProperty(PropertyName = "parent_order_acceptance_id")]
    public virtual string ParentOrderAcceptanceId { get; set; }

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
    /// List Parent Orders
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetParentOrders">Online help</see>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="productCode"></param>
    /// <param name="orderState"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<T[]>> GetParentOrdersAsync<T>(string productCode, BfOrderState orderState, long count, long before, long after, CancellationToken ct) where T : BfParentOrderStatus
    {
        var query = string.Format("product_code={0}{1}{2}{3}",
            productCode,
            orderState != BfOrderState.Unknown ? "&parent_order_state=" + orderState.ToEnumString() : "",
            (count > 0)  ? $"&count={count}"   : "",
            (before > 0) ? $"&before={before}" : "",
            (after > 0)  ? $"&after={after}"   : ""
        );

        return GetPrivateAsync<T[]>(nameof(GetParentOrdersAsync), query, ct);
    }

    /// <summary>
    /// List Parent Orders
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetParentOrders">Online help</see>
    /// </summary>
    /// <param name="productCode"></param>
    /// <param name="orderState"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfParentOrderStatus[]>> GetParentOrdersAsync(string productCode, BfOrderState orderState, long count, long before, long after, CancellationToken ct)
        => GetParentOrdersAsync<BfParentOrderStatus>(productCode, orderState, count, before, after, ct);

    /// <summary>
    /// List Parent Orders
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetParentOrders">Online help</see>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="productCode"></param>
    /// <param name="orderState"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public async Task<T[]> GetParentOrdersAsync<T>(string productCode, BfOrderState orderState = BfOrderState.Unknown, long count = 0, long before = 0, long after = 0) where T : BfParentOrderStatus
        => (await GetParentOrdersAsync<T>(productCode, orderState, count, before, after, CancellationToken.None)).Deserialize();

    /// <summary>
    /// List Parent Orders
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetParentOrders">Online help</see>
    /// </summary>
    /// <param name="productCode"></param>
    /// <param name="orderState"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public async Task<BfParentOrderStatus[]> GetParentOrdersAsync(string productCode, BfOrderState orderState = BfOrderState.Unknown, long count = 0, long before = 0, long after = 0)
        => (await GetParentOrdersAsync<BfParentOrderStatus>(productCode, orderState, count, before, after, CancellationToken.None)).Deserialize();
}
