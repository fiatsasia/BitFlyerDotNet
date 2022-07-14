//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public static class BfOrderFactory
{
    public static BfChildOrder Market(string productCode, BfTradeSide side, decimal size, TimeSpan? minuteToExpire = default, BfTimeInForce? timeInForce = default)
        => new()
        {
            ProductCode = productCode,
            ChildOrderType = BfOrderType.Market,
            Side = side,
            Size = size,
            MinuteToExpire = minuteToExpire.HasValue ? Convert.ToInt32(minuteToExpire.Value.TotalMinutes) : default,
            TimeInForce = timeInForce,
        };

    public static BfChildOrder Limit(string productCode, BfTradeSide side, decimal price, decimal size, TimeSpan? minuteToExpire = default, BfTimeInForce? timeInForce = default)
        => new()
        {
            ProductCode = productCode,
            ChildOrderType = BfOrderType.Limit,
            Side = side,
            Size = size,
            Price = price,
            MinuteToExpire = minuteToExpire.HasValue ? Convert.ToInt32(minuteToExpire.Value.TotalMinutes) : default,
            TimeInForce = timeInForce,
        };

    public static BfParentOrder Stop(string productCode, BfTradeSide side, decimal triggerPrice, decimal size, TimeSpan? minuteToExpire = default, BfTimeInForce? timeInForce = default)
    {
        if (size > triggerPrice)
        {
            throw new ArgumentException();
        }

        return new()
        {
            OrderMethod = BfOrderType.Simple,
            Parameters = new()
            {
                new() {
                    ProductCode = productCode,
                    ConditionType = BfOrderType.Stop,
                    Side = side,
                    TriggerPrice = triggerPrice,
                    Size = size,
                }
            },
            MinuteToExpire = minuteToExpire.HasValue ? Convert.ToInt32(minuteToExpire.Value.TotalMinutes) : default,
            TimeInForce = timeInForce,
        };
    }

    public static BfParentOrder StopLimit(string productCode, BfTradeSide side, decimal triggerPrice, decimal price, decimal size, TimeSpan? minuteToExpire = default, BfTimeInForce? timeInForce = default)
    {
        if (size > price || size > triggerPrice)
        {
            throw new ArgumentException();
        }

        return new()
        {
            OrderMethod = BfOrderType.Simple,
            Parameters = new()
            {
                new() {
                    ProductCode = productCode,
                    ConditionType = BfOrderType.StopLimit,
                    Side = side,
                    Price = price,
                    Size = size,
                    TriggerPrice = triggerPrice
                }
            },
            MinuteToExpire = minuteToExpire.HasValue ? Convert.ToInt32(minuteToExpire.Value.TotalMinutes) : default,
            TimeInForce = timeInForce,
        };
    }

    public static BfParentOrder Trail(string productCode, BfTradeSide side, decimal offset, decimal size, TimeSpan? minuteToExpire = default, BfTimeInForce? timeInForce = default)
    {
        if (offset <= 0m)
        {
            throw new ArgumentException();
        }

        return new()
        {
            OrderMethod = BfOrderType.Simple,
            Parameters = new()
            {
                new() {
                    ProductCode = productCode,
                    ConditionType = BfOrderType.Trail,
                    Side = side,
                    Offset = offset,
                    Size = size,
                }
            },
            MinuteToExpire = minuteToExpire.HasValue ? Convert.ToInt32(minuteToExpire.Value.TotalMinutes) : default,
            TimeInForce = timeInForce,
        };
    }

    public static BfParentOrder IFD(BfParentOrderParameter first, BfParentOrderParameter second, TimeSpan? minuteToExpire = default, BfTimeInForce? timeInForce = default)
        => new()
        {
            OrderMethod = BfOrderType.IFD,
            Parameters = new() { first, second },
            MinuteToExpire = minuteToExpire.HasValue ? Convert.ToInt32(minuteToExpire.Value.TotalMinutes) : default,
            TimeInForce = timeInForce,
        };

    public static BfParentOrder OCO(BfParentOrderParameter first, BfParentOrderParameter second, TimeSpan? minuteToExpire = default, BfTimeInForce? timeInForce = default)
    {
        if (first.ConditionType == second.ConditionType && first.Side == second.Side)
        {
            throw new ArgumentException("OCO child orders should not be same."); // Ordering limitations will start at Dec/2/2020
        }
        return new()
        {
            OrderMethod = BfOrderType.OCO,
            Parameters = new() { first, second },
            MinuteToExpire = minuteToExpire.HasValue ? Convert.ToInt32(minuteToExpire.Value.TotalMinutes) : default,
            TimeInForce = timeInForce,
        };
    }

    public static BfParentOrder IFDOCO(BfParentOrderParameter ifdone, BfParentOrderParameter ocoFirst, BfParentOrderParameter ocoSecond, TimeSpan? minuteToExpire = default, BfTimeInForce? timeInForce = default)
    {
        if (ocoFirst.ConditionType == ocoSecond.ConditionType && ocoFirst.Side == ocoSecond.Side)
        {
            throw new ArgumentException("OCO child orders should not be same."); // Ordering limitations will start at Dec/2/2020
        }
        return new()
        {
            OrderMethod = BfOrderType.IFDOCO,
            Parameters = new() { ifdone, ocoFirst, ocoSecond },
            MinuteToExpire = minuteToExpire.HasValue ? Convert.ToInt32(minuteToExpire.Value.TotalMinutes) : default,
            TimeInForce = timeInForce,
        };
    }
}
