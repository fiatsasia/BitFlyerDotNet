//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfPrivateExecution : IBfPagingElement
{
    [JsonProperty(PropertyName = "id")]
    public virtual long Id { get; set; }

    [JsonProperty(PropertyName = "child_order_id")]
    public virtual string ChildOrderId { get; set; }

    [JsonProperty(PropertyName = "side")]
    [JsonConverter(typeof(StringEnumConverter))]
    public virtual BfTradeSide Side { get; set; }

    [JsonProperty(PropertyName = "price")]
    public virtual decimal Price { get; set; }

    [JsonProperty(PropertyName = "size")]
    public virtual decimal Size { get; set; }

    [JsonProperty(PropertyName = "commission")]
    public virtual decimal Commission { get; set; }

    [JsonProperty(PropertyName = "exec_date")]
    public virtual DateTime ExecDate { get; set; }

    [JsonProperty(PropertyName = "child_order_acceptance_id")]
    public virtual string ChildOrderAcceptanceId { get; set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// List Executions
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetPrivateExecutions">Online help</see>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="productCode"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="childOrderId"></param>
    /// <param name="childOrderAcceptanceId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<T[]>> GetPrivateExecutionsAsync<T>(string productCode, long count, long before, long after, string childOrderId, string childOrderAcceptanceId, CancellationToken ct) where T : BfPrivateExecution
    {
        var query = string.Format("product_code={0}{1}{2}{3}{4}{5}",
            productCode,
            (count > 0)  ? $"&count={count}"   : "",
            (before > 0) ? $"&before={before}" : "",
            (after > 0)  ? $"&after={after}"   : "",
            !string.IsNullOrEmpty(childOrderId) ? "&child_order_id=" + childOrderId : "",
            !string.IsNullOrEmpty(childOrderAcceptanceId) ? "&child_order_acceptance_id=" + childOrderAcceptanceId : ""
        );

        return GetPrivateAsync<T[]>("getexecutions", query, ct);
    }

    /// <summary>
    /// List Executions
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetPrivateExecutions">Online help</see>
    /// </summary>
    /// <param name="productCode"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="childOrderId"></param>
    /// <param name="childOrderAcceptanceId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfPrivateExecution[]>> GetPrivateExecutionsAsync(string productCode, long count, long before, long after, string childOrderId, string childOrderAcceptanceId, CancellationToken ct)
        => GetPrivateExecutionsAsync<BfPrivateExecution>(productCode, count, before, after, childOrderId, childOrderAcceptanceId, ct);

    /// <summary>
    /// List Executions
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetPrivateExecutions">Online help</see>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="productCode"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="childOrderId"></param>
    /// <param name="childOrderAcceptanceId"></param>
    /// <returns></returns>
    public async Task<T[]> GetPrivateExecutionsAsync<T>(string productCode, long count = 0, long before = 0, long after = 0, string childOrderId = null, string childOrderAcceptanceId = null) where T : BfPrivateExecution
        => (await GetPrivateExecutionsAsync<T>(productCode, count, before, after, childOrderId, childOrderAcceptanceId, CancellationToken.None)).Deserialize();

    /// <summary>
    /// List Executions
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetPrivateExecutions">Online help</see>
    /// </summary>
    /// <param name="productCode"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="childOrderId"></param>
    /// <param name="childOrderAcceptanceId"></param>
    /// <returns></returns>
    public async Task<BfPrivateExecution[]> GetPrivateExecutionsAsync(string productCode, long count = 0, long before = 0, long after = 0, string childOrderId = null, string childOrderAcceptanceId = null)
        => (await GetPrivateExecutionsAsync<BfPrivateExecution>(productCode, count, before, after, childOrderId, childOrderAcceptanceId, CancellationToken.None)).Deserialize();
}
