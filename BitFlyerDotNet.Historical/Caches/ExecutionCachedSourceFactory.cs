//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Collections.Concurrent;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public class ExecutionCachedSourceFactory
    {
        ConcurrentDictionary<BfProductCode, ConcurrentDictionary<int, ExecutionCachedSource>> _sources = new ConcurrentDictionary<BfProductCode, ConcurrentDictionary<int, ExecutionCachedSource>>();

        BitFlyerClient _client;
        ICacheFactory _cacheFactory;

        public ExecutionCachedSourceFactory(BitFlyerClient client, ICacheFactory cacheFactory)
        {
            _client = client;
            _cacheFactory = cacheFactory;
        }

        public IObservable<IBfExecution> GetExecutionCachedSource(BfProductCode productCode, int before)
        {
            return _sources.GetOrAdd(productCode, _ => { return new ConcurrentDictionary<int, ExecutionCachedSource>(); })
            .GetOrAdd(before, __ =>
            {
                return new ExecutionCachedSource(_client, _cacheFactory.GetExecutionCache(productCode), productCode, before);
            });
        }
    }
}
