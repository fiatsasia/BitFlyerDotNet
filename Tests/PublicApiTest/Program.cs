//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BitFlyerDotNet.LightningApi;

namespace PublicApiTest
{
    class Program
    {
        static BitFlyerClient _client;

        static void Main(string[] args)
        {
            _client = new BitFlyerClient();

            GetMarkets();

            Console.ReadLine();
        }

        static void GetMarkets()
        {
            var resp = _client.GetMarkets();
            foreach (var market in resp.GetResult())
            {
                Console.WriteLine("{0} {1}", market.ProductCode, market.Alias);
            }
        }
    }
}
