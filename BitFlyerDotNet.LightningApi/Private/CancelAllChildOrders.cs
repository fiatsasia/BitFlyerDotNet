//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfCancelAllChildOrdersRequest
    {
        public string ProductCode { get; set; }
    }

    public partial class BitFlyerClient
    {
        /// <summary>
        /// Cancel All Orders
        /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelAllChildOrders">Online help</see>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<BitFlyerResponse<string>> CancelAllChildOrdersAsync(BfCancelAllChildOrdersRequest request, CancellationToken ct)
        {
            return PostPrivateAsync<string>(nameof(CancelAllChildOrders), request, ct);
        }

        public BitFlyerResponse<string> CancelAllChildOrders(BfCancelAllChildOrdersRequest request)
            => CancelAllChildOrdersAsync(request, CancellationToken.None).Result;

        /// <summary>
        /// Cancel All Orders
        /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelAllChildOrders">Online help</see>
        /// </summary>
        /// <param name="productCode"></param>
        /// <returns></returns>
        public Task<BitFlyerResponse<string>> CancelAllChildOrdersAsync(string productCode, CancellationToken ct)
            => CancelAllChildOrdersAsync(new BfCancelAllChildOrdersRequest { ProductCode = productCode }, ct);

        public BitFlyerResponse<string> CancelAllChildOrders(string productCode)
            => CancelAllChildOrdersAsync(productCode, CancellationToken.None).Result;
    }
}
