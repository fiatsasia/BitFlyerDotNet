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
        void Validate(ref BfCancelChildOrderRequest request)
        {
            if (string.IsNullOrEmpty(request.ChildOrderId) && string.IsNullOrEmpty(request.ChildOrderAcceptanceId))
            {
                throw new ArgumentException();
            }
        }

        /// <summary>
        /// Cancel Order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelChildOrder">Online help</see>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<BitFlyerResponse<string>> CancelChildOrderAsync(BfCancelChildOrderRequest request, CancellationToken ct)
        {
            Validate(ref request);
            return PostPrivateAsync<string>(nameof(CancelChildOrder), request, ct);
        }

        /// <summary>
        /// Cancel Order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelChildOrder">Online help</see>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public BitFlyerResponse<string> CancelChildOrder(BfCancelChildOrderRequest request) => CancelChildOrderAsync(request, CancellationToken.None).Result;

        /// <summary>
        /// Cancel Order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelChildOrder">Online help</see>
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
            Validate(ref request);
            return PostPrivateAsync<string>(nameof(CancelChildOrder), request, ct);
        }

        /// <summary>
        /// Cancel Order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelChildOrder">Online help</see>
        /// </summary>
        /// <param name="productCode"></param>
        /// <param name="childOrderId"></param>
        /// <param name="childOrderAcceptanceId"></param>
        /// <returns></returns>
        public BitFlyerResponse<string> CancelChildOrder(string productCode, string childOrderId = null, string childOrderAcceptanceId = null)
            => CancelChildOrderAsync(productCode, childOrderId, childOrderAcceptanceId, CancellationToken.None).Result;
    }
}
