//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Generic;

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

        public static async IAsyncEnumerable<(BfProductCode ProductCode, string Symbol)> GetAvailableMarketsAsync(this BitFlyerClient client)
        {
            await foreach (var markets in client.GetMarketsAllAsync().SelectAwait(async (e) => (await e).GetContent()))
            {
                foreach (var market in markets)
                {
                    if (market.ProductCode.StartsWith("BTCJPY"))
                    {
                        if (string.IsNullOrEmpty(market.Alias))
                        {
                            continue; // ******** BTCJPY future somtimes missing alias, skip it ********
                        }
                        yield return ((BfProductCode)Enum.Parse(typeof(BfProductCode), market.Alias.Replace("_", "")), market.ProductCode);
                    }
                    else
                    {
                        yield return ((BfProductCode)Enum.Parse(typeof(BfProductCode), market.ProductCode.Replace("_", "")), market.ProductCode);
                    }
                }
            }
        }
    }
}
