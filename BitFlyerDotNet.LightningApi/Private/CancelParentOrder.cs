//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
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
        /// <summary>
        /// Cancel parent order
        /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelParentOrder">Online help</see>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public BitFlyerResponse<string> CancelParentOrder(BfCancelParentOrderRequest request)
        {
            return PrivatePost<string>(nameof(CancelParentOrder), request);
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
            if (string.IsNullOrEmpty(parentOrderId) && string.IsNullOrEmpty(parentOrderAcceptanceId))
            {
                throw new ArgumentException();
            }

            return CancelParentOrder(new BfCancelParentOrderRequest
            {
                ProductCode = productCode,
                ParentOrderId = parentOrderId,
                ParentOrderAcceptanceId = parentOrderAcceptanceId
            });
        }
    }
}
