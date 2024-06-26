﻿//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

class CountTimerLimitter
{
    TimeSpan _interval;
    int _limitCount;
    ConcurrentQueue<DateTime> _queue;
    DateTime _last;

    public CountTimerLimitter(TimeSpan interval, int limitCount)
    {
        _interval = interval;
        _limitCount = limitCount;
        _queue = new ();
        _last = DateTime.Now;
    }

    public bool CheckLimitReached()
    {
        var currentTime = DateTime.Now;
        if (currentTime - _last >= _interval)
        {
            while (_queue.TryDequeue(out DateTime result)) ; // Clear queue
        }
        _last = currentTime;

        _queue.Enqueue(currentTime);
        if (_queue.Count <= _limitCount)
        {
            return false;
        }

        _queue.TryDequeue(out var oldestTime);
        return (currentTime - oldestTime) <= _interval;
    }

    public bool IsLimitReached {
        get
        {
            if (_queue.Count < _limitCount)
            {
                return false;
            }

            if (!_queue.TryPeek(out DateTime result))
            {
                return false;
            }

            return ((DateTime.Now - result) <= _interval);
        }
    }
}
