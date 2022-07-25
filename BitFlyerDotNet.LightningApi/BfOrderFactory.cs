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
    #region Child orders
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
    #endregion Child orders

    #region Simple parent orders
    public static BfParentOrder Stop(string productCode, BfTradeSide side, decimal triggerPrice, decimal size, TimeSpan? minuteToExpire = default, BfTimeInForce? timeInForce = default)
    {
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
    #endregion Simple parent orders

    #region Conditional parent orders
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
        return new()
        {
            OrderMethod = BfOrderType.IFDOCO,
            Parameters = new() { ifdone, ocoFirst, ocoSecond },
            MinuteToExpire = minuteToExpire.HasValue ? Convert.ToInt32(minuteToExpire.Value.TotalMinutes) : default,
            TimeInForce = timeInForce,
        };
    }
    #endregion Conditional parent orders

    #region Verify orders
    public static IBfOrder Verify(this IBfOrder order) => order switch { BfChildOrder co => co.Verify(), BfParentOrder po => po.Verify(), _ => throw new ArgumentException() };

    public static BfChildOrder Verify(this BfChildOrder order)
    {
        if (!order.ChildOrderType.IsChildOrderType())
        {
            throw new ArgumentException($"child order: Illegal child order type {order.ChildOrderType}");
        }

        if (order.Side != BfTradeSide.Buy && order.Side != BfTradeSide.Sell)
        {
            throw new ArgumentException($"child order: Illegal trade side {order.Side}");
        }

        if (order.Size < BfProductCode.GetMinimumOrderSize(order.ProductCode))
        {
            throw new ArgumentException($"child order: illegal order size {order.Size}");
        }

        switch (order.ChildOrderType)
        {
            case BfOrderType.Market:
                if (order.Price.HasValue)
                {
                    throw new ArgumentException($"child order: market order price must be null.");
                }
                break;

            case BfOrderType.Limit:
                if (!order.Price.HasValue)
                {
                    throw new ArgumentException($"child order: limit order price must not be null.");
                }
                break;
        }

        return order;
    }

    static void Verify(BfParentOrderParameter order)
    {
        if (!order.ConditionType.IsConditionType())
        {
            throw new ArgumentException($"parent order: Illegal child condition type {order.ConditionType}");
        }

        if (order.Side != BfTradeSide.Buy && order.Side != BfTradeSide.Sell)
        {
            throw new ArgumentException($"parent order: Illegal trade side {order.Side}");
        }

        if (order.Size < BfProductCode.GetMinimumOrderSize(order.ProductCode))
        {
            throw new ArgumentException($"parent order: illegal order size {order.Size}");
        }

        switch (order.ConditionType)
        {
            case BfOrderType.Market:
                if (order.Price.HasValue || order.TriggerPrice.HasValue || order.Offset.HasValue)
                {
                    throw new ArgumentException($"parent order: price, trigger prie and offset must be null.");
                }
                break;

            case BfOrderType.Limit:
                if (!order.Price.HasValue || order.TriggerPrice.HasValue || order.Offset.HasValue)
                {
                    throw new ArgumentException($"parent order: price must not be null.");
                }
                break;

            case BfOrderType.Stop:
                if (!order.TriggerPrice.HasValue || order.Price.HasValue || order.Offset.HasValue)
                {
                    throw new ArgumentException($"parent order: trigger price must not be null. price and offset must be null.");
                }
                break;

            case BfOrderType.StopLimit:
                if (!order.TriggerPrice.HasValue || !order.Price.HasValue || order.Offset.HasValue)
                {
                    throw new ArgumentException("parent order: trigger and order price must not be null. offset must be null.");
                }
                break;

            case BfOrderType.Trail:
                if (!order.Offset.HasValue || order.Offset.Value < decimal.Zero || order.Price.HasValue || order.TriggerPrice.HasValue)
                {
                    throw new ArgumentException("parent order: offset value must not be null and positive value. order price and trigger price must be null.");
                }
                break;

            default:
                throw new ArgumentException($"parent order: Unknown condition type '{order.ConditionType}'.");
        }
    }

    public static BfParentOrder Verify(this BfParentOrder order)
    {
        if (!order.OrderMethod.IsOrderMethod())
        {
            throw new ArgumentException($"parent order: Illegal order method {order.OrderMethod}");
        }

        if (order.Parameters.Select(e => e.ProductCode).Distinct().Count() > 1)
        {
            throw new ArgumentException("parent order: all of product code must be same.");
        }

        switch (order.OrderMethod)
        {
            case BfOrderType.Simple:
                if (!order.Parameters[0].ConditionType.IsSimpleConditionType())
                {
                    throw new ArgumentException($"parent order: Illegal child condition type {order.Parameters[0].ConditionType}");
                }
                Verify(order.Parameters[0]);
                break;

            default:
                switch (order.OrderMethod)
                {
                    case BfOrderType.OCO:
                        {
                            var first = order.Parameters[0];
                            var second = order.Parameters[1];
                            if (first.ConditionType == second.ConditionType && first.Side == second.Side)
                            {
                                throw new ArgumentException("parent order: OCO child orders should not be same."); // Ordering limitations will start at Dec/2/2020
                            }
                        }
                        break;

                    case BfOrderType.IFDOCO:
                        {
                            var ocoFirst = order.Parameters[1];
                            var ocoSecond = order.Parameters[2];
                            if (ocoFirst.ConditionType == ocoSecond.ConditionType && ocoFirst.Side == ocoSecond.Side)
                            {
                                throw new ArgumentException("OCO child orders should not be same."); // Ordering limitations will start at Dec/2/2020
                            }
                        }
                        break;
                }
                order.Parameters.ForEach(e => Verify(e));
                break;
        }
        return order;
    }
    #endregion Verify orders
}
