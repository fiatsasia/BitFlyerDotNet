//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Threading;
using System.Threading.Tasks;

namespace BitFlyerDotNet.LightningApi
{
    public class BfCancelChildOrderRequest
    {
        public string ProductCode { get; set; }

        public string ChildOrderId { get; set; }
        public bool ShouldSerializeChildOrderId() { return !string.IsNullOrEmpty(ChildOrderId); }

        public string ChildOrderAcceptanceId { get; set; }
        public bool ShouldSerializeChildOrderAcceptanceId() { return !string.IsNullOrEmpty(ChildOrderAcceptanceId); }
    }

    public partial class BitFlyerClient
    {
        /// <summary>
        /// Cancel Order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelChildOrderAsync">Online help</see>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<BitFlyerResponse<string>> CancelChildOrderAsync(string productCode, string childOrderId, string childOrderAcceptanceId, CancellationToken ct)
        {
            var request = new BfCancelChildOrderRequest
            {
                ProductCode = productCode,
                ChildOrderId = childOrderId,
                ChildOrderAcceptanceId = childOrderAcceptanceId
            };
            return PostPrivateAsync<string>(nameof(CancelChildOrderAsync), request, ct);
        }

        public async Task<bool> CancelChildOrderAsync(string productCode, string childOrderId = null, string childOrderAcceptanceId = null)
            => (await CancelChildOrderAsync(productCode, childOrderId, childOrderAcceptanceId, CancellationToken.None)).IsOk;
    }
}
