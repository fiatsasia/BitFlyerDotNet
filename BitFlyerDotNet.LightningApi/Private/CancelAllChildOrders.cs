//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfCancelAllChildOrdersRequest
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public BfProductCode ProductCode { get; set; }
    }

    public partial class BitFlyerClient
    {
        /// <summary>
        /// Cancel All Orders
        /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelAllChildOrders">Online help</see>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public BitFlyerResponse<string> CancelAllChildOrders(BfCancelAllChildOrdersRequest request)
        {
            return PrivatePost<string>(nameof(CancelAllChildOrders), request);
        }

        /// <summary>
        /// Cancel All Orders
        /// <see href="https://scrapbox.io/BitFlyerDotNet/CancelAllChildOrders">Online help</see>
        /// </summary>
        /// <param name="productCode"></param>
        /// <returns></returns>
        public BitFlyerResponse<string> CancelAllChildOrders(BfProductCode productCode)
        {
            return CancelAllChildOrders(new BfCancelAllChildOrdersRequest
            {
                ProductCode = productCode,
            });
        }
    }
}
