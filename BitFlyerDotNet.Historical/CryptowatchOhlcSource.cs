//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Financial.Extensions;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public static class CryptowatchOhlcSource
    {
        const string _baseUri = "https://api.cryptowat.ch";
        const string _basePath = "/markets/bitflyer";


        static Dictionary<BfProductCode, string> _productSymbols = new Dictionary<BfProductCode, string>
        {
            { BfProductCode.BTCJPY, "btcjpy" },
            { BfProductCode.ETHBTC, "ethbtc" },
            { BfProductCode.BCHBTC, "bchbtc" },
            { BfProductCode.FXBTCJPY, "btcfxjpy" },
            { BfProductCode.BTCUSD, "btcusd" },
            { BfProductCode.BTCEUR, "btceur" },
            { BfProductCode.BTCJPYMAT1WK, "btcjpy-weekly-futures" },
            { BfProductCode.BTCJPYMAT2WK, "btcjpy-biweekly-futures" },
            { BfProductCode.BTCJPYMAT3M, "btcjpy-quarterly-futures" },
            { BfProductCode.ETHJPY, "ethjpy" },
        };
        internal static readonly List<TimeSpan> SupportedFrameSpans = new List<TimeSpan>
        {
            { TimeSpan.FromMinutes(1) },
            { TimeSpan.FromMinutes(3) },
            { TimeSpan.FromMinutes(5) },
            { TimeSpan.FromMinutes(15) },
            { TimeSpan.FromMinutes(30) },
            { TimeSpan.FromHours(1) },
            { TimeSpan.FromHours(2) },
            { TimeSpan.FromHours(4) },
            { TimeSpan.FromHours(6) },
            { TimeSpan.FromHours(12) },
            { TimeSpan.FromDays(1) },
            { TimeSpan.FromDays(3) },
            { TimeSpan.FromDays(7) },
        };
        public static bool IsSupportedFrameSpan(TimeSpan frameSpan)
        {
            return SupportedFrameSpans.Contains(frameSpan);
        }

        static HttpClient _client;

        static CryptowatchOhlcSource()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri(_baseUri);
        }

        class Ohlc : IFxOhlcvv
        {
            public DateTime Start { get; set; }
            public decimal Open { get; set; }
            public decimal High { get; set; }
            public decimal Low { get; set; }
            public decimal Close { get; set; }
            public double Volume { get; set; }
            public double VWAP { get; set; }
        }

        static readonly Ohlc[] _errorResult = new Ohlc[0];
        static IEnumerable<Ohlc> JsonDeserialize(TimeSpan frameSpan, string json)
        {
            var ohlcs = new List<Ohlc>();
            var result = JsonConvert.DeserializeObject<JObject>(json)["result"][frameSpan.TotalSeconds.ToString()];
            foreach (var element in result)
            {
                var ohlc = new Ohlc();
                var closeTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse((string)element[0])).UtcDateTime;
                ohlc.Open = decimal.Parse((string)element[1]);
                ohlc.High = decimal.Parse((string)element[2]);
                ohlc.Low = decimal.Parse((string)element[3]);
                ohlc.Close = decimal.Parse((string)element[4]);
                ohlc.Volume = double.Parse((string)element[5]);
                ohlc.VWAP = double.Parse((string)element[6]) / ohlc.Volume;
                ohlc.Start = closeTime - frameSpan;
                ohlcs.Add(ohlc);
            }
            return ohlcs;
        }

        public static IEnumerable<IFxOhlcvv> Get(BfProductCode productCode, TimeSpan frameSpan, DateTime beforeClose, DateTime afterClose)
        {
            if (!_productSymbols.ContainsKey(productCode))
            {
                throw new NotSupportedException(string.Format("Product '{0}' is not supported.", productCode));
            }
            if (!SupportedFrameSpans.Contains(frameSpan))
            {
                throw new NotSupportedException(string.Format("Frame span '{0}' is not supported.", frameSpan));
            }
            
            // API accepts "Close Time" based range.
            var path = string.Format("{0}/{1}/ohlc?periods={2}&before={3}&after={4}",
                _basePath,
                _productSymbols[productCode],
                Convert.ToInt64(frameSpan.TotalSeconds),
                new DateTimeOffset(beforeClose).ToUnixTimeSeconds(),
                new DateTimeOffset(afterClose).ToUnixTimeSeconds()
            );

            using (var request = new HttpRequestMessage(HttpMethod.Get, path))
            {
                try
                {
                    var message = _client.SendAsync(request).Result;
                    if (!message.IsSuccessStatusCode)
                    {
                        return _errorResult;
                    }
                    return JsonDeserialize(frameSpan, message.Content.ReadAsStringAsync().Result);
                }
                catch (AggregateException)
                {
                    return _errorResult;
                }
            }
        }
    }
}
