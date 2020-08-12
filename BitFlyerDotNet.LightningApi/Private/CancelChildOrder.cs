//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfCancelChildOrderRequest
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public BfProductCode ProductCode { get; set; }

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
        public BitFlyerResponse<string> CancelChildOrder(BfCancelChildOrderRequest request)
        {
            Validate(ref request);
            return PrivatePostAsync<string>(nameof(CancelChildOrder), request, CancellationToken.None).Result;
        }

        /// <summary>
        /// Cancel Order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelChildOrder">Online help</see>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BitFlyerResponse<string>> CancelChildOrderAsync(BfCancelChildOrderRequest request, CancellationToken ct)
        {
            Validate(ref request);
            return await PrivatePostAsync<string>(nameof(CancelChildOrder), request, ct);
        }

        /// <summary>
        /// Cancel Order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelChildOrder">Online help</see>
        /// </summary>
        /// <param name="productCode"></param>
        /// <param name="childOrderId"></param>
        /// <param name="childOrderAcceptanceId"></param>
        /// <returns></returns>
        public BitFlyerResponse<string> CancelChildOrder(BfProductCode productCode, string childOrderId = null, string childOrderAcceptanceId = null)
        {
            var request = new BfCancelChildOrderRequest
            {
                ProductCode = productCode,
                ChildOrderId = childOrderId,
                ChildOrderAcceptanceId = childOrderAcceptanceId
            };
            Validate(ref request);
            return PrivatePostAsync<string>(nameof(CancelChildOrder), request, CancellationToken.None).Result;
        }

        /// <summary>
        /// Cancel Order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelChildOrder">Online help</see>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BitFlyerResponse<string>> CancelChildOrderAsync(BfProductCode productCode, string childOrderId, string childOrderAcceptanceId, CancellationToken ct)
        {
            var request = new BfCancelChildOrderRequest
            {
                ProductCode = productCode,
                ChildOrderId = childOrderId,
                ChildOrderAcceptanceId = childOrderAcceptanceId
            };
            Validate(ref request);
            return await PrivatePostAsync<string>(nameof(CancelChildOrder), request, ct);
        }
    }
}
