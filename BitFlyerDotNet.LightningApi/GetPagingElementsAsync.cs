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

    public IAsyncEnumerable<T> GetExecutionsAsync<T>(
        string productCode,
        long count, long before, long after,
        Func<T, bool> predicate,
        CancellationToken ct
    ) where T : BfExecution
       => GetPagingElementsAsync(
           (count, before, after, ct) => GetExecutionsAsync<T>(productCode, count, before, after, ct),
           count, before, after,
           predicate,
           ct
       );

    public IAsyncEnumerable<T> GetBalanceHistoryAsync<T>(
        string currencyCode,
        long count, long before, long after,
        Func<T, bool> predicate,
        CancellationToken ct
    ) where T : BfBalanceHistory
       => GetPagingElementsAsync(
           (count, before, after, ct) => GetBalanceHistoryAsync<T>(currencyCode, count, before, after, ct),
           count, before, after,
           predicate,
           ct
       );

    public IAsyncEnumerable<T> GetChildOrdersAsync<T>(
        string productCode,
        BfOrderState orderState,
        long count, long before, long after,
        string childOrderId,
        string childOrderAcceptanceId,
        string parentOrderId,
        Func<T, bool> predicate,
        CancellationToken ct
    ) where T : BfChildOrderStatus
        => GetPagingElementsAsync(
            (count, before, after, ct) => GetChildOrdersAsync<T>(productCode, orderState, count, before, after, childOrderId, childOrderAcceptanceId, parentOrderId, ct),
            count, before, after,
            predicate,
            ct
        );

    public IAsyncEnumerable<T> GetCoinInsAsync<T>(
        long count, long before, long after,
        Func<T, bool> predicate,
        CancellationToken ct
    ) where T : BfCoinin
       => GetPagingElementsAsync(GetCoinInsAsync<T>, count, before, after, predicate, ct);

    public IAsyncEnumerable<T> GetCoinOutsAsync<T>(
        long count, long before, long after,
        Func<T, bool> predicate, CancellationToken ct
    ) where T : BfCoinOut
       => GetPagingElementsAsync(GetCoinOutsAsync<T>, count, before, after, predicate, ct);

    public IAsyncEnumerable<T> GetCollateralHistoryAsync<T>(
        long count, long before, long after,
        Func<T, bool> predicate,
        CancellationToken ct
    ) where T : BfCollateralHistory
       => GetPagingElementsAsync(GetCollateralHistoryAsync<T>, count, before, after, predicate, ct);

    public IAsyncEnumerable<T> GetDepositsAsync<T>(
        long count, long before, long after,
        Func<T, bool> predicate,
        CancellationToken ct
    ) where T : BfDeposit
       => GetPagingElementsAsync(GetDepositsAsync<T>, count, before, after, predicate, ct);

    public IAsyncEnumerable<BfParentOrderStatus> GetParentOrdersAsync(
        string productCode,
        BfOrderState orderState,
        long count, long before, long after,
        Func<BfParentOrderStatus, bool> predicate,
        CancellationToken ct
    )
        => GetPagingElementsAsync(
            (count, before, after, ct) => GetParentOrdersAsync<BfParentOrderStatus>(productCode, orderState, count, before, after, ct),
            count, before, after,
            predicate,
            ct
        );

    public IAsyncEnumerable<T> GetParentOrdersAsync<T>(
        string productCode,
        BfOrderState orderState,
        long count, long before, long after,
        Func<T, bool> predicate,
        CancellationToken ct
    ) where T : BfParentOrderStatus
        => GetPagingElementsAsync(
            (count, before, after, ct) => GetParentOrdersAsync<T>(productCode, orderState, count, before, after, ct),
            count, before, after,
            predicate,
            ct
        );

    public IAsyncEnumerable<T> GetPrivateExecutionsAsync<T>(
        string productCode,
        long count, long before, long after,
        string childOrderId,
        string childOrderAcceptanceId,
        Func<T, bool> predicate,
        CancellationToken ct
    ) where T : BfPrivateExecution
        => GetPagingElementsAsync(
            (count, before, after, ct) => GetPrivateExecutionsAsync<T>(productCode, count, before, after, childOrderId, childOrderAcceptanceId, ct),
            count, before, after,
            predicate,
            ct
        );

    public IAsyncEnumerable<T> GetWithdrawalsAsync<T>(
        string messageId,
        long count, long before, long after,
        Func<T, bool> predicate,
        CancellationToken ct
    ) where T : BfWithdrawal
       => GetPagingElementsAsync(
           (count, before, after, ct) => GetWithdrawalsAsync<T>(messageId, count, before, after, ct),
           count, before, after,
           predicate,
           ct
       );
}
