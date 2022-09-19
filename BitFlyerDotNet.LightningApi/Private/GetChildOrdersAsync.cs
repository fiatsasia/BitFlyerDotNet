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
    public long Id { get; private set; }

    [JsonProperty(PropertyName = "child_order_id")]
    public string ChildOrderId { get; private set; }

    [JsonProperty(PropertyName = "product_code")]
    public string ProductCode { get; private set; }

    [JsonProperty(PropertyName = "side")]
    [JsonConverter(typeof(StringEnumConverter))]
    public BfTradeSide Side { get; private set; }

    [JsonProperty(PropertyName = "child_order_type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public BfOrderType ChildOrderType { get; private set; }

    [JsonProperty(PropertyName = "price")]
    public decimal? Price { get; private set; }

    [JsonProperty(PropertyName = "average_price")]
    public decimal AveragePrice { get; private set; }

    [JsonProperty(PropertyName = "size")]
    public decimal Size { get; private set; }

    [JsonProperty(PropertyName = "child_order_state")]
    [JsonConverter(typeof(StringEnumConverter))]
    public BfOrderState ChildOrderState { get; private set; }

    [JsonProperty(PropertyName = "expire_date")]
    public DateTime ExpireDate { get; private set; }

    [JsonProperty(PropertyName = "child_order_date")]
    public DateTime ChildOrderDate { get; private set; }

    [JsonProperty(PropertyName = "child_order_acceptance_id")]
    public string ChildOrderAcceptanceId { get; private set; }

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
    /// List child orders with result status
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
    )
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

        return GetPrivateAsync<BfChildOrderStatus[]>(nameof(GetChildOrdersAsync), query, ct);
    }

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
    public async Task<BfChildOrderStatus[]> GetChildOrdersAsync(
        string productCode,
        BfOrderState orderState = BfOrderState.Unknown,
        long count = 0L,
        long before = 0L,
        long after = 0L,
        string childOrderId = null,
        string childOrderAcceptanceId = null,
        string parentOrderId = null
    ) => (await GetChildOrdersAsync(productCode, orderState, count, before, after, childOrderId, childOrderAcceptanceId, parentOrderId, CancellationToken.None)).GetContent();
}
