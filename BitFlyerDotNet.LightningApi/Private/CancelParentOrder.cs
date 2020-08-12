﻿//==============================================================================
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
    public class BfCancelParentOrderRequest
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public BfProductCode ProductCode { get; set; }

        public string ParentOrderId { get; set; }
        public bool ShouldSerializeParentOrderId() { return !string.IsNullOrEmpty(ParentOrderId); }

        public string ParentOrderAcceptanceId { get; set; }
        public bool ShouldSerializeParentOrderAcceptanceId() { return !string.IsNullOrEmpty(ParentOrderAcceptanceId); }
    }

    public partial class BitFlyerClient
    {
        void Validate(ref BfCancelParentOrderRequest request)
        {
            if (string.IsNullOrEmpty(request.ParentOrderId) && string.IsNullOrEmpty(request.ParentOrderAcceptanceId))
            {
                throw new ArgumentException();
            }
        }

        /// <summary>
        /// Cancel parent order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelParentOrder">Online help</see>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public BitFlyerResponse<string> CancelParentOrder(BfCancelParentOrderRequest request)
        {
            Validate(ref request);
            return PrivatePostAsync<string>(nameof(CancelParentOrder), request, CancellationToken.None).Result;
        }

        /// <summary>
        /// Cancel parent order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelParentOrder">Online help</see>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<BitFlyerResponse<string>> CancelParentOrderAsync(BfCancelParentOrderRequest request, CancellationToken ct)
        {
            Validate(ref request);
            return await PrivatePostAsync<string>(nameof(CancelParentOrder), request, ct);
        }

        /// <summary>
        /// Cancel parent order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelParentOrder">Online help</see>
        /// </summary>
        /// <param name="productCode"></param>
        /// <param name="parentOrderId"></param>
        /// <param name="parentOrderAcceptanceId"></param>
        /// <returns></returns>
        public BitFlyerResponse<string> CancelParentOrder(BfProductCode productCode, string parentOrderId = null, string parentOrderAcceptanceId = null)
        {
            var request = new BfCancelParentOrderRequest
            {
                ProductCode = productCode,
                ParentOrderId = parentOrderId,
                ParentOrderAcceptanceId = parentOrderAcceptanceId
            };
            Validate(ref request);
            return PrivatePostAsync<string>(nameof(CancelParentOrder), request, CancellationToken.None).Result;
        }

        /// <summary>
        /// Cancel parent order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelParentOrder">Online help</see>
        /// </summary>
        /// <param name="productCode"></param>
        /// <param name="parentOrderId"></param>
        /// <param name="parentOrderAcceptanceId"></param>
        /// <returns></returns>
        public async Task<BitFlyerResponse<string>> CancelParentOrderAsync(BfProductCode productCode, string parentOrderId, string parentOrderAcceptanceId, CancellationToken ct)
        {
            var request = new BfCancelParentOrderRequest
            {
                ProductCode = productCode,
                ParentOrderId = parentOrderId,
                ParentOrderAcceptanceId = parentOrderAcceptanceId
            };
            Validate(ref request);
            return await PrivatePostAsync<string>(nameof(CancelParentOrder), request, ct);
        }
    }
}
