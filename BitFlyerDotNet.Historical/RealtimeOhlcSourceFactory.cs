//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
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
