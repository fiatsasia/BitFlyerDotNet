//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitFlyerDotNet.LightningApi
{
    public static class BitFlyerClientExtensions
    {
        public static IEnumerable<(BfProductCode ProductCode, string Symbol)> GetAvailableMarkets(this BitFlyerClient client)
        {
            var result = new List<(BfProductCode ProductCode, string Symbol)>();
            foreach (var market in client.GetMarketsAll().SelectMany(e => e.GetContent()))
            {
                if (market.ProductCode.StartsWith("BTCJPY"))
                {
                    if (string.IsNullOrEmpty(market.Alias))
                    {
                        continue; // ******** BTCJPY future somtimes missing alias, skip it ********
                    }
                    result.Add(((BfProductCode)Enum.Parse(typeof(BfProductCode), market.Alias.Replace("_", "")), market.ProductCode));
                }
                else
                {
                    result.Add(((BfProductCode)Enum.Parse(typeof(BfProductCode), market.ProductCode.Replace("_", "")), market.ProductCode));
                }
            }
            return result.Distinct(e => e.ProductCode);
        }

        public static async Task<IEnumerable<(BfProductCode ProductCode, string Symbol)>> GetAvailableMarketsAsync(this BitFlyerClient client)
        {
            var result = new List<(BfProductCode ProductCode, string Symbol)>();
            foreach (var task in client.GetMarketsAllAsync())
            {
                var markets = (await task).GetContent();
                foreach (var market in markets)
                {
                    if (market.ProductCode.StartsWith("BTCJPY"))
                    {
                        if (string.IsNullOrEmpty(market.Alias))
                        {
                            continue; // ******** BTCJPY future somtimes missing alias, skip it ********
                        }
                        result.Add(((BfProductCode)Enum.Parse(typeof(BfProductCode), market.Alias.Replace("_", "")), market.ProductCode));
                    }
                    else
                    {
                        result.Add(((BfProductCode)Enum.Parse(typeof(BfProductCode), market.ProductCode.Replace("_", "")), market.ProductCode));
                    }
                }
            }
            return result.Distinct(e => e.ProductCode);
        }
    }
}
