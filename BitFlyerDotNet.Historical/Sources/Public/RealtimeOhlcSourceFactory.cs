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
    public class RealtimeOhlcSourceFactory
    {
        RealtimeSourceFactory _realtimeFactory;
        ExecutionCachedSourceFactory _historicalFactory;

        public RealtimeOhlcSourceFactory(RealtimeSourceFactory realtimeFactory, ExecutionCachedSourceFactory historicalFactory)
        {
            _realtimeFactory = realtimeFactory;
            _historicalFactory = historicalFactory;
        }

        public IObservable<RealtimeOhlc> GetRealtimeOhlcSource(BfProductCode productCode, TimeSpan frameSpan)
        {
            return new RealtimeOhlcSource(_historicalFactory, productCode, frameSpan, _realtimeFactory.GetExecutionSource(productCode, true));
        }
    }
}
