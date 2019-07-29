//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public static class BfOrderConvertExtension
    {
        public static BfParentOrderRequestParameter ToParameter(this BfChildOrderRequest child)
        {
            return new BfParentOrderRequestParameter
            {
                ProductCode = child.ProductCode,
                ConditionType = child.OrderType,
                Side = child.Side,
                Size = child.Size,
                Price = child.Price,
            };
        }

        public static BfParentOrderRequestParameter ToParameter(this BfParentOrderRequest parent)
        {
            if (parent.Paremters.Count != 1)
            {
                throw new ArgumentException();
            }
            return parent.Paremters[0];
        }
    }

    public class BfxOrderFactory
    {
        BfTradingMarket _market;

        public BfxOrderFactory(BfTradingMarket market)
        {
            _market = market;
        }

        public BfChildOrderRequest CreateMarketPriceOrder(BfTradeSide side, decimal size)
        {
            var request = new BfChildOrderRequest
            {
                ProductCode = _market.ProductCode,
                OrderType = BfOrderType.Market,
                Side = side,
                Size = size,
            };
            CheckChildOrderRequestValid(request);
            return request;
        }

        public BfChildOrderRequest CreateLimitPriceOrder(BfTradeSide side, decimal size, decimal price)
        {
            var request = new BfChildOrderRequest
            {
                ProductCode = _market.ProductCode,
                OrderType = BfOrderType.Limit,
                Side = side,
                Size = size,
                Price = price,
            };

            CheckChildOrderRequestValid(request);
            return request;
        }

        public BfChildOrderRequest CreateLimitPriceOrder(BfTradeSide side, decimal size, decimal price, TimeSpan minuteToExpire)
        {
            var request = new BfChildOrderRequest
            {
                ProductCode = _market.ProductCode,
                OrderType = BfOrderType.Limit,
                Side = side,
                Size = size,
                Price = price,
            };
            request.MinuteToExpireSpan = minuteToExpire;

            CheckChildOrderRequestValid(request);
            return request;
        }

        public BfChildOrderRequest CreateLimitPriceOrder(BfTradeSide side, decimal size, decimal price, BfTimeInForce timeInForce)
        {
            var request = new BfChildOrderRequest
            {
                ProductCode = _market.ProductCode,
                OrderType = BfOrderType.Limit,
                Side = side,
                Size = size,
                Price = price,
            };
            request.TimeInForce = timeInForce;

            CheckChildOrderRequestValid(request);
            return request;
        }

        public BfChildOrderRequest CreateLimitPriceOrder(BfTradeSide side, decimal size, decimal price, TimeSpan minuteToExpire, BfTimeInForce timeInForce)
        {
            var request = new BfChildOrderRequest
            {
                ProductCode = _market.ProductCode,
                OrderType = BfOrderType.Limit,
                Side = side,
                Size = size,
                Price = price,
            };
            request.MinuteToExpireSpan = minuteToExpire;
            request.TimeInForce = timeInForce;

            CheckChildOrderRequestValid(request);
            return request;
        }

        public BfParentOrderRequest CreateStopOrder(BfTradeSide side, decimal size, decimal stopTriggerPrice)
        {
            if (size > stopTriggerPrice)
            {
                throw new ArgumentException();
            }
            var request = new BfParentOrderRequest { OrderMethod = BfOrderType.Simple };
            request.Paremters.Add(new BfParentOrderRequestParameter { ProductCode = _market.ProductCode, ConditionType = BfOrderType.Stop, Side = side, Size = size });
            CheckParentOrderRequestValid(request);
            return request;
        }

        public BfParentOrderRequest CreateStopLimitOrder(BfTradeSide side, decimal size, decimal price, decimal stopTriggerPrice)
        {
            if (size > price || size > stopTriggerPrice)
            {
                throw new ArgumentException();
            }
            var request = new BfParentOrderRequest { OrderMethod = BfOrderType.Simple };
            request.Paremters.Add(new BfParentOrderRequestParameter { ProductCode = _market.ProductCode, ConditionType = BfOrderType.StopLimit, Side = side, Size = size, Price = price, TriggerPrice = stopTriggerPrice });
            CheckParentOrderRequestValid(request);
            return request;
        }

        public BfParentOrderRequest CreateTrailOrder(BfTradeSide side, decimal size, decimal trailingStopPriceOffset)
        {
            if (size > trailingStopPriceOffset)
            {
                throw new ArgumentException();
            }
            var request = new BfParentOrderRequest { OrderMethod = BfOrderType.Simple };
            request.Paremters.Add(new BfParentOrderRequestParameter { ProductCode = _market.ProductCode, ConditionType = BfOrderType.Trail, Side = side, Size = size, Offset = trailingStopPriceOffset });
            CheckParentOrderRequestValid(request);
            return request;
        }

        public BfParentOrderRequest CreateIFD(BfParentOrderRequestParameter first, BfParentOrderRequestParameter second, TimeSpan? minuteToExpire = null, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            var request = new BfParentOrderRequest { OrderMethod = BfOrderType.IFD };
            if (minuteToExpire.HasValue)
            {
                request.MinuteToExpire = (int)minuteToExpire.Value.TotalMinutes;
            }
            request.TimeInForce = timeInForce;
            request.Paremters.Add(first);
            request.Paremters.Add(second);
            CheckParentOrderRequestValid(request);
            return request;
        }

        public BfParentOrderRequest CreateOCO(BfParentOrderRequestParameter first, BfParentOrderRequestParameter second, TimeSpan? minuteToExpire = null, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            var request = new BfParentOrderRequest { OrderMethod = BfOrderType.OCO };
            if (minuteToExpire.HasValue)
            {
                request.MinuteToExpire = (int)minuteToExpire.Value.TotalMinutes;
            }
            request.TimeInForce = timeInForce;
            request.Paremters.Add(first);
            request.Paremters.Add(second);
            CheckParentOrderRequestValid(request);
            return request;
        }

        public BfParentOrderRequest CreateIFDOCO(BfParentOrderRequestParameter ifdone, BfParentOrderRequestParameter ocoFirst, BfParentOrderRequestParameter ocoSecond, TimeSpan? minuteToExpire = null, BfTimeInForce timeInForce = BfTimeInForce.NotSpecified)
        {
            var request = new BfParentOrderRequest { OrderMethod = BfOrderType.IFDOCO };
            if (minuteToExpire.HasValue)
            {
                request.MinuteToExpire = (int)minuteToExpire.Value.TotalMinutes;
            }
            request.TimeInForce = timeInForce;
            request.Paremters.Add(ifdone);
            request.Paremters.Add(ocoFirst);
            request.Paremters.Add(ocoSecond);
            CheckParentOrderRequestValid(request);
            return request;
        }

        public void CheckChildOrderRequestValid(IBfChildOrderRequest request)
        {
            if (request.OrderType != BfOrderType.Market && request.Size > request.Price)
            {
                throw new ArgumentException();
            }

            if (request.Size < _market.MinimumOrderSize)
            {
                throw new ArgumentException("Request size must be greater equal 0.01");
            }
        }

        public void CheckParentOrderRequestValid(BfParentOrderRequest request)
        {
            foreach (var childRequest in request.Paremters)
            {
                if (request.Paremters[0].ProductCode != childRequest.ProductCode)
                {
                    throw new ArgumentException("Different product code is set to child order");
                }

                CheckChildOrderRequestValid(childRequest);

                switch (request.OrderMethod)
                {
                    case BfOrderType.Simple:
                        switch (childRequest.ConditionType)
                        {
                            case BfOrderType.Stop:
                            case BfOrderType.StopLimit:
                            case BfOrderType.Trail:
                                break;

                            default:
                                throw new ArgumentException("Illegal child order type");
                        }
                        break;

                    case BfOrderType.IFD:
                    case BfOrderType.OCO:
                    case BfOrderType.IFDOCO:
                        switch (childRequest.ConditionType)
                        {
                            case BfOrderType.Limit:
                            case BfOrderType.Market:
                            case BfOrderType.Stop:
                            case BfOrderType.StopLimit:
                            case BfOrderType.Trail:
                                break;

                            default:
                                throw new ArgumentException("Illegal child order type");
                        }
                        break;

                    default:
                        throw new ArgumentException("Illegal parent order type");
                }
            }
        }
    }
}
