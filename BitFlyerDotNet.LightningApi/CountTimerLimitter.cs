//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Concurrent;

namespace BitFlyerDotNet.LightningApi
{
    class CountTimerLimitter
    {
        TimeSpan _interval;
        int _limitCount;
        ConcurrentQueue<DateTime> _queue;

        public CountTimerLimitter(TimeSpan interval, int limitCount)
        {
            _interval = interval;
            _limitCount = limitCount;
            _queue = new ConcurrentQueue<DateTime>();
        }

        public bool CheckLimitReached()
        {
            var currentTime = DateTime.Now;
            _queue.Enqueue(currentTime);
            if (_queue.Count < _limitCount)
            {
                return false;
            }

            _queue.TryDequeue(out var oldestTime);
            return (currentTime - oldestTime) < _interval;
        }
    }
}
