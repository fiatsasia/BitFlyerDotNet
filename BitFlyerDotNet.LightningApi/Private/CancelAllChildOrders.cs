//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System.Threading;
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
            return PostPrivateAsync<string>(nameof(CancelAllChildOrders), request, CancellationToken.None).Result;
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
