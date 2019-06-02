//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using Financial.Extensions;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public class HistoricalOhlcSourceFactory
    {
        ICacheFactory _cacheFactory;
        string _cacheFolderBasePath;

        public HistoricalOhlcSourceFactory(ICacheFactory cacheFactory, string cacheFolderBasePath)
        {
            _cacheFactory = cacheFactory;
            _cacheFolderBasePath = cacheFolderBasePath;
        }

        public IObservable<IFxOhlcvv> GetHistoricalOhlcSource(BfProductCode productCode, TimeSpan frameSpan, DateTime endFrom, TimeSpan span)
        {
            return new HistoricalOhlcSource(_cacheFactory, productCode, frameSpan, endFrom, span, _cacheFolderBasePath);
        }
    }
}
