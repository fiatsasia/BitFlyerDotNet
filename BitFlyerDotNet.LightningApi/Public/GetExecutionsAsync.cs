//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BfExecution : IBfPagingElement
{
    [JsonProperty(PropertyName = "id")]
    public virtual long Id { get; set; }

    [JsonProperty(PropertyName = "side")]
    [JsonConverter(typeof(StringEnumConverter))]
    public virtual BfTradeSide Side { get; set; }

    [JsonProperty(PropertyName = "price")]
    public virtual decimal Price { get; set; }

    [JsonProperty(PropertyName = "size")]
    public virtual decimal Size { get; set; }

    [JsonProperty(PropertyName = "exec_date")]
    public virtual DateTime ExecDate { get; set; }

    [JsonProperty(PropertyName = "buy_child_order_acceptance_id")]
    public virtual string BuyChildOrderAcceptanceId { get; set; }

    [JsonProperty(PropertyName = "sell_child_order_acceptance_id")]
    public virtual string SellChildOrderAcceptanceId { get; set; }
}

public partial class BitFlyerClient
{
    /// <summary>
    /// List Executions
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetExecutions">Online help</see>
    /// </summary>
    /// <param name="productCode"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<T[]>> GetExecutionsAsync<T>(string productCode, long count, long before, long after, CancellationToken ct) where T : BfExecution
    {
        var query = string.Format("product_code={0}{1}{2}{3}",
            productCode,
            (count > 0) ? $"&count={count}" : "",
            (before > 0) ? $"&before={before}" : "",
            (after > 0) ? $"&after={after}" : ""
        );
        return GetAsync<T[]>(nameof(GetExecutionsAsync), query, ct);
    }

    /// <summary>
    /// List Executions
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetExecutions">Online help</see>
    /// </summary>
    /// <param name="productCode"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<BitFlyerResponse<BfExecution[]>> GetExecutionsAsync(string productCode, long count, long before, long after, CancellationToken ct)
        => GetExecutionsAsync<BfExecution>(productCode, count, before, after, ct);

    /// <summary>
    /// List Executions
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetExecutions">Online help</see>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="productCode"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public async Task<T[]> GetExecutionsAsync<T>(string productCode, long count = 0, long before = 0, long after = 0) where T : BfExecution
        => (await GetExecutionsAsync<T>(productCode, count, before, after, CancellationToken.None)).Deserialize();

    /// <summary>
    /// List Executions
    /// <see href="https://scrapbox.io/BitFlyerDotNet/GetExecutions">Online help</see>
    /// </summary>
    /// <param name="productCode"></param>
    /// <param name="count"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public async Task<BfExecution[]> GetExecutionsAsync(string productCode, long count = 0, long before = 0, long after = 0)
        => (await GetExecutionsAsync<BfExecution>(productCode, count, before, after, CancellationToken.None)).Deserialize();
}
