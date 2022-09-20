//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public partial class BitFlyerClient
{
    async IAsyncEnumerable<T> GetPagingElementsAsync<T>(
        Func<long, long, long, CancellationToken, Task<BitFlyerResponse<T[]>>> getMethod,
        long count, long before, long after,
        Func<T, bool> predicate,
        CancellationToken ct
    ) where T : IBfPagingElement
    {
        var readCount = Math.Min(count, ReadCountMax);
        if (count == 0)
        {
            count = int.MaxValue;
        }
        while (true)
        {
            var resp = await getMethod(readCount, before, 0, ct);
            if (resp.IsError)
            {
                switch (resp.StatusCode)
                {
                    case HttpStatusCode.BadRequest: // no more records
                        yield break;

                    case HttpStatusCode.InternalServerError:
                        await Task.Delay(30 * 1000); // Probably server is in maintanace. wait 30 secs
                        break;
                }
                continue;
            }

            var elements = resp.Deserialize();
            if (elements.Length == 0)
            {
                break;
            }

            foreach (var element in elements)
            {
                if (!(predicate?.Invoke(element) ?? true))
                {
                    yield break;
                }
                else if (element.Id <= after)
                {
                    yield break;
                }
                else if (count-- == 0)
                {
                    yield break;
                }

                yield return element;
            }

            if (elements.Length < readCount)
            {
                break;
            }

            before = elements.Last().Id;
        }
    }

    public IAsyncEnumerable<BfExecution> GetExecutionsAsync(string productCode, long count, long before, long after, Func<BfExecution, bool> predicate, CancellationToken ct)
       => GetPagingElementsAsync(
           (count, before, after, ct) => GetExecutionsAsync(productCode, count, before, after, ct),
           count, before, after,
           predicate,
           ct
       );

    public IAsyncEnumerable<BfBalanceHistory> GetBalanceHistoryAsync(string currencyCode, long count, long before, long after, Func<BfBalanceHistory, bool> predicate, CancellationToken ct)
       => GetPagingElementsAsync(
           (count, before, after, ct) => GetBalanceHistoryAsync(currencyCode, count, before, after, ct),
           count, before, after,
           predicate,
           ct
       );

    public IAsyncEnumerable<BfChildOrderStatus> GetChildOrdersAsync(
        string productCode,
        BfOrderState orderState,
        long count, long before, long after,
        string childOrderId,
        string childOrderAcceptanceId,
        string parentOrderId,
        Func<BfChildOrderStatus, bool> predicate,
        CancellationToken ct
    )
        => GetPagingElementsAsync(
            (count, before, after, ct) => GetChildOrdersAsync(productCode, orderState, count, before, after, childOrderId, childOrderAcceptanceId, parentOrderId, ct),
            count, before, after,
            predicate,
            ct
        );

    public IAsyncEnumerable<BfCoinin> GetCoinInsAsync(long count, long before, long after, Func<BfCoinin, bool> predicate, CancellationToken ct)
       => GetPagingElementsAsync(GetCoinInsAsync, count, before, after, predicate, ct);

    public IAsyncEnumerable<BfCoinOut> GetCoinOutsAsync(long count, long before, long after, Func<BfCoinOut, bool> predicate, CancellationToken ct)
       => GetPagingElementsAsync(GetCoinOutsAsync, count, before, after, predicate, ct);

    public IAsyncEnumerable<BfCollateralHistory> GetCollateralHistoryAsync(long count, long before, long after, Func<BfCollateralHistory, bool> predicate, CancellationToken ct)
       => GetPagingElementsAsync(GetCollateralHistoryAsync, count, before, after, predicate, ct);

    public IAsyncEnumerable<BfDeposit> GetDepositsAsync(long count, long before, long after, Func<BfDeposit, bool> predicate, CancellationToken ct)
       => GetPagingElementsAsync(GetDepositsAsync, count, before, after, predicate, ct);

    public IAsyncEnumerable<BfParentOrderStatus> GetParentOrdersAsync(string productCode, BfOrderState orderState, long count, long before, long after, Func<BfParentOrderStatus, bool> predicate, CancellationToken ct)
        => GetPagingElementsAsync(
            (count, before, after, ct) => GetParentOrdersAsync(productCode, orderState, count, before, after, ct),
            count, before, after,
            predicate,
            ct
        );

    public IAsyncEnumerable<BfPrivateExecution> GetPrivateExecutionsAsync(
        string productCode,
        long count, long before, long after,
        string childOrderId,
        string childOrderAcceptanceId,
        Func<BfPrivateExecution, bool> predicate,
        CancellationToken ct
    )
        => GetPagingElementsAsync(
            (count, before, after, ct) => GetPrivateExecutionsAsync(productCode, count, before, after, childOrderId, childOrderAcceptanceId, ct),
            count, before, after,
            predicate,
            ct
        );

    public IAsyncEnumerable<BfWithdrawal> GetWithdrawalsAsync(string messageId, long count, long before, long after, Func<BfWithdrawal, bool> predicate, CancellationToken ct)
       => GetPagingElementsAsync(
           (count, before, after, ct) => GetWithdrawalsAsync(messageId, count, before, after, ct),
           count, before, after,
           predicate,
           ct
       );
}
