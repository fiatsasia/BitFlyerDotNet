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
    public static BfChildOrder Market(string productCode, BfTradeSide side, decimal size, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        => new()
        {
            ProductCode = productCode,
            ChildOrderType = BfOrderType.Market,
            Side = side,
            Size = size,
            MinuteToExpire = minuteToExpire,
            TimeInForce = timeInForce,
        };

    public static BfChildOrder Limit(string productCode, BfTradeSide side, decimal price, decimal size, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        => new()
        {
            ProductCode = productCode,
            ChildOrderType = BfOrderType.Limit,
            Side = side,
            Size = size,
            Price = price,
            MinuteToExpire = minuteToExpire,
            TimeInForce = timeInForce,
        };

    public static BfParentOrder Stop(string productCode, BfTradeSide side, decimal triggerPrice, decimal size, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
    {
        if (size > triggerPrice)
        {
            throw new ArgumentException();
        }

        return new()
        {
            OrderMethod = BfOrderType.Simple,
            MinuteToExpire = minuteToExpire,
            TimeInForce = timeInForce,
            Parameters = new()
            {
                new() {
                    ProductCode = productCode,
                    ConditionType = BfOrderType.Stop,
                    Side = side,
                    TriggerPrice = triggerPrice,
                    Size = size,
                }
            }
        };
    }

    public static BfParentOrder StopLimit(string productCode, BfTradeSide side, decimal triggerPrice, decimal price, decimal size, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
    {
        if (size > price || size > triggerPrice)
        {
            throw new ArgumentException();
        }

        return new()
        {
            OrderMethod = BfOrderType.Simple,
            MinuteToExpire = minuteToExpire,
            TimeInForce = timeInForce,
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
            }
        };
    }

    public static BfParentOrder Trail(string productCode, BfTradeSide side, decimal offset, decimal size, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
    {
        if (offset <= 0m)
        {
            throw new ArgumentException();
        }

        return new()
        {
            OrderMethod = BfOrderType.Simple,
            MinuteToExpire = minuteToExpire,
            TimeInForce = timeInForce,
            Parameters = new()
            {
                new() {
                    ProductCode = productCode,
                    ConditionType = BfOrderType.Trail,
                    Side = side,
                    Offset = offset,
                    Size = size,
                }
            }
        };
    }

    public static BfParentOrder IFD(BfParentOrderParameter first, BfParentOrderParameter second, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        => new()
        {
            OrderMethod = BfOrderType.IFD,
            MinuteToExpire = minuteToExpire,
            TimeInForce = timeInForce,
            Parameters = new() { first, second }
        };

    public static BfParentOrder OCO(BfParentOrderParameter first, BfParentOrderParameter second, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
    {
        if (first.ConditionType == second.ConditionType && first.Side == second.Side)
        {
            throw new ArgumentException("OCO child orders should not be same."); // Ordering limitations will start at Dec/2/2020
        }
        return new()
        {
            OrderMethod = BfOrderType.OCO,
            MinuteToExpire = minuteToExpire,
            TimeInForce = timeInForce,
            Parameters = new() { first, second }
        };
    }

    public static BfParentOrder IFDOCO(BfParentOrderParameter ifdone, BfParentOrderParameter ocoFirst, BfParentOrderParameter ocoSecond, int minuteToExpire = 0, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
    {
        if (ocoFirst.ConditionType == ocoSecond.ConditionType && ocoFirst.Side == ocoSecond.Side)
        {
            throw new ArgumentException("OCO child orders should not be same."); // Ordering limitations will start at Dec/2/2020
        }
        return new()
        {
            OrderMethod = BfOrderType.IFDOCO,
            MinuteToExpire = minuteToExpire,
            TimeInForce = timeInForce,
            Parameters = new() { ifdone, ocoFirst, ocoSecond }
        };
    }
}
