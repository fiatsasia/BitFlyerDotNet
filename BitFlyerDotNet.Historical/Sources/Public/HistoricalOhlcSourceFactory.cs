//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
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

        public IObservable<IOhlcvv> GetHistoricalOhlcSource(BfProductCode productCode, TimeSpan frameSpan, DateTime endFrom, TimeSpan span)
        {
            return new HistoricalOhlcSource(_cacheFactory, productCode, frameSpan, endFrom, span, _cacheFolderBasePath);
        }
    }
}
