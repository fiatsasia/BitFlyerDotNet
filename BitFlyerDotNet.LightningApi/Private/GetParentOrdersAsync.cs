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
    public long Id { get; private set; }

    [JsonProperty(PropertyName = "parent_order_id")]
    public string ParentOrderId { get; private set; }

    [JsonProperty(PropertyName = "product_code")]
    public string ProductCode { get; private set; }

    [JsonProperty(PropertyName = "side")]
    [JsonConverter(typeof(StringEnumConverter))]
    public BfTradeSide Side { get; private set; }

    [JsonProperty(PropertyName = "parent_order_type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public BfOrderType ParentOrderType { get; private set; } // if request is simple, this contains children[0]

    [JsonProperty(PropertyName = "price")]
    public decimal? Price { get; private set; }

    [JsonProperty(PropertyName = "average_price")]
    public decimal? AveragePrice { get; private set; }

    [JsonProperty(PropertyName = "size")]
    public decimal? Size { get; private set; }

    [JsonProperty(PropertyName = "parent_order_state")]
    [JsonConverter(typeof(StringEnumConverter))]
    public BfOrderState ParentOrderState { get; private set; }

    [JsonProperty(PropertyName = "expire_date")]
    public DateTime ExpireDate { get; private set; }

    [JsonProperty(PropertyName = "parent_order_date")]
    public DateTime ParentOrderDate { get; private set; }

    [JsonProperty(PropertyName = "parent_order_acceptance_id")]
    public string ParentOrderAcceptanceId { get; private set; }

    [JsonProperty(PropertyName = "outstanding_size")]
    public decimal OutstandingSize { get; private set; }

    [JsonProperty(PropertyName = "cancel_size")]
    public decimal CancelSize { get; private set; }

    [JsonProperty(PropertyName = "executed_size")]
    public decimal ExecutedSize { get; private set; }

    [JsonProperty(PropertyName = "total_commission")]
    public decimal TotalCommission { get; private set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// List parent orders with result
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetParentOrders">Online help</see>
    /// </summary>
    /// <param name="productCode"></param>
    /// <param name="orderState"></param>
    /// <param name="count">Default is 100</param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfParentOrderStatus[]>> GetParentOrdersAsync(string productCode, BfOrderState orderState, long count, long before, long after, CancellationToken ct)
    {
        var query = string.Format("product_code={0}{1}{2}{3}",
            productCode,
            orderState != BfOrderState.Unknown ? "&parent_order_state=" + orderState.ToEnumString() : "",
            (count > 0)  ? $"&count={count}"   : "",
            (before > 0) ? $"&before={before}" : "",
            (after > 0)  ? $"&after={after}"   : ""
        );

        return GetPrivateAsync<BfParentOrderStatus[]>(nameof(GetParentOrdersAsync), query, ct);
    }

    /// <summary>
    /// List parent orders
    /// </summary>
    /// <param name="productCode"></param>
    /// <param name="orderState"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public async Task<BfParentOrderStatus[]> GetParentOrdersAsync(string productCode, BfOrderState orderState = BfOrderState.Unknown, long count = 0, long before = 0, long after = 0)
        => (await GetParentOrdersAsync(productCode, orderState, count, before, after, CancellationToken.None)).Deserialize();
}
