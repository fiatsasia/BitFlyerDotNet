//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All right reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Concurrent;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public class HistoricalExecutionSourceFactory
    {
        ConcurrentDictionary<BfProductCode, ConcurrentDictionary<int, HistoricalExecutionSource>> _sources = new ConcurrentDictionary<BfProductCode, ConcurrentDictionary<int, HistoricalExecutionSource>>();

        BitFlyerClient _client;
        string _cacheFolderBasePath;

        public HistoricalExecutionSourceFactory(BitFlyerClient client, string cacheFolderBasePath)
        {
            _client = client;
            _cacheFolderBasePath = cacheFolderBasePath;
        }

        public IObservable<IBfExecution> GetHistoricalExecutionSource(BfProductCode productCode, int before)
        {
            return _sources.GetOrAdd(productCode, _ => { return new ConcurrentDictionary<int, HistoricalExecutionSource>(); })
            .GetOrAdd(before, __ =>
            {
                return new HistoricalExecutionSource(_client, productCode, before, _cacheFolderBasePath);
            });
        }
    }
}
