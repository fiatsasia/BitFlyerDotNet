//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All right reserved.
// https://www.fiats.asia/
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public class HistoricalOhlcSourceFactory
    {
        string _cacheFolderBasePath;

        public HistoricalOhlcSourceFactory(string cacheFolderBasePath)
        {
            _cacheFolderBasePath = cacheFolderBasePath;
        }

        public IObservable<IBfOhlc> GetHistoricalOhlcSource(BfProductCode productCode, TimeSpan frameSpan, DateTime endFrom, TimeSpan span)
        {
            return new HistoricalOhlcSource(productCode, frameSpan, endFrom, span, _cacheFolderBasePath);
        }
    }
}
