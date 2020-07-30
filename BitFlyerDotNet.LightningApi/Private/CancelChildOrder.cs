//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
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
        /// <summary>
        /// Cancel Order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelChildOrder">Online help</see>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public BitFlyerResponse<string> CancelChildOrder(BfCancelChildOrderRequest request)
        {
            if (string.IsNullOrEmpty(request.ChildOrderId) && string.IsNullOrEmpty(request.ChildOrderAcceptanceId))
            {
                throw new ArgumentException();
            }

            return PrivatePost<string>(nameof(CancelChildOrder), request);
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
            return CancelChildOrder(new BfCancelChildOrderRequest
            {
                ProductCode = productCode,
                ChildOrderId = childOrderId,
                ChildOrderAcceptanceId = childOrderAcceptanceId
            });
        }
    }
}
