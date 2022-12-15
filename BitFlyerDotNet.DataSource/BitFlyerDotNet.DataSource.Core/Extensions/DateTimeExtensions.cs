//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.DataSource
{
    public static class DateTimeExtensions
    {
        public static DateTime Round(this DateTime dt, TimeSpan unit)
        {
            return new DateTime(dt.Ticks / unit.Ticks * unit.Ticks, dt.Kind);
        }
    }
}
