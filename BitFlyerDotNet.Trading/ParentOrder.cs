//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Reactive.Disposables;
using Fiats.Utils;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    internal class ParentOrder : IBfTradeOrder
    {
        ITradeAccount _account;
        CompositeDisposable _disposables = new CompositeDisposable();

        // Prooperties
        public bool IsSimple { get { return _request.OrderMethod == BfParentOrderMethod.Simple; } }
        public BfProductCode ProductCode { get { return _request.Paremters[0].ProductCode; } }
        public BfOrderType OrderType { get { return IsSimple ? _request.Paremters[0].ConditionType : BfOrderType.Unknown; } }
        public BfTradeSide Side { get { return IsSimple ? _request.Paremters[0].Side : BfTradeSide.Unknown; } }
        public object Tag { get; set; }

        public DateTime OrderCreatedTime { get; private set; }
        public double OrderSize { get { return IsSimple ? _request.Paremters[0].Size : 0.0; } }
        public double OrderPrice { get { return IsSimple ? _request.Paremters[0].Price : 0.0; } }
        public double TriggerPrice { get { return IsSimple ? _request.Paremters[0].TriggerPrice : 0.0; } }
        public double LimitOffset { get { return IsSimple ? _request.Paremters[0].Offset : 0.0; } }

        BfParentOrderRequest _request;
        BitFlyerResponse<BfParentOrderResponse> _response;
        BfParentOrder _parentOrder;
        BfChildOrder[] _childOrders;
        BfTicker _ticker;

        public double ExecutedPrice { get; }
        public double ExecutedSize { get; }
        public DateTime OrderRequestedTime { get; }
        public DateTime OrderAcceptedTime { get; private set; }
        public double ReferencePrice { get { return _ticker.MidPrice; } }

        public DateTime ExecutedTime { get; }

        public bool IsExecuted { get { return false; } }
        public bool IsCompleted { get { return false; } }
        public bool IsCancelable { get { return false; } }

        public bool IsError { get { return _response.IsError; } }
        public BfOrderState PrimitiveStatus { get { return (_parentOrder == null) ? BfOrderState.Unknown : _parentOrder.ParentOrderState; } }

        public int MinuteToExpire { get { return _request.MinuteToExpire; } set { _request.MinuteToExpire = value; } }
        public BfTimeInForce TimeInForce { get { return _request.TimeInForce; } set { _request.TimeInForce = value; } }

        public string ParentOrderAcceptanceId { get { return _response.GetResult().ParentOrderAcceptanceId; } }

        public BfTradeOrderState Status { get; private set; }
        void UpdateStatus(BfTradeOrderState status)
        {
        }

        public event OrderStatusChangedCallback StatusChanged;

        Timer _orderConfirmPollingTimer;
        public TimeSpan OrderConfirmPollingInterval { get; set; } = TimeSpan.FromSeconds(3);

        public ParentOrder(ITradeAccount account, BfOrderType orderType, BfTradeSide side, double size, double price = 0.0, double triggerPrice = 0.0, double limitOffset = 0.0)
        {
            _account = account;
            OrderCreatedTime = _account.ServerTime;
            StatusChanged += _account.OnOrderStatusChanged;

            _request = new BfParentOrderRequest
            {
                OrderMethod = BfParentOrderMethod.Simple,
                MinuteToExpire = _account.MinuteToExpire,
                TimeInForce = _account.TimeInForce,
            };

            _request.Paremters.Add(new BfParentOrderRequestParameter
            {
                ProductCode = _account.ProductCode,
                ConditionType = orderType,
                Side = side,
                Size = size,
                Price = price,
                TriggerPrice = triggerPrice,
                Offset = limitOffset,
            });
        }

        public ParentOrder(ITradeAccount account, BfParentOrderMethod method, IBfTradeOrder[] orders)
        {
            _account = account;
            OrderCreatedTime = _account.ServerTime;
            StatusChanged += _account.OnOrderStatusChanged;

            _request = new BfParentOrderRequest
            {
                OrderMethod = method,
                MinuteToExpire = _account.MinuteToExpire,
                TimeInForce = _account.TimeInForce,
            };

            foreach (var order in orders)
            {
                if (!order.IsSimple)
                {
                    throw new ArgumentException();
                }

                _request.Paremters.Add(new BfParentOrderRequestParameter
                {
                    ProductCode = order.ProductCode,
                    ConditionType = order.OrderType,
                    Side = order.Side,
                    Size = order.OrderSize,
                    Price = order.OrderPrice,
                    TriggerPrice = order.TriggerPrice,
                    Offset = order.LimitOffset,
                });
            }
        }

        public bool Send(BitFlyerClient client)
        {
            _response = client.SendParentOrder(_request);
            if (_response.IsError)
            {
                UpdateStatus(BfTradeOrderState.OrderFailed);
                return false;
            }

            OrderAcceptedTime = _account.ServerTime;
            UpdateStatus(BfTradeOrderState.OrderAccepted);

            _orderConfirmPollingTimer = new Timer(OnConfirmPollingTimerExired, client, TimeSpan.Zero, OrderConfirmPollingInterval).AddTo(_disposables);
            return true;
        }

        void OnConfirmPollingTimerExired(object state)
        {
            DebugEx.EnterMethod();
            var client = state as BitFlyerClient;
            _orderConfirmPollingTimer.Change(Timeout.Infinite, Timeout.Infinite); // stop timer

            if (!Confirm(client) || !IsCompleted)
            {
                _orderConfirmPollingTimer.Change(OrderConfirmPollingInterval, OrderConfirmPollingInterval); // restart timer
                DebugEx.ExitMethod();
                return;
            }
            DebugEx.ExitMethod();
        }

        public bool Confirm(BitFlyerClient client)
        {
            var resp = client.GetChildOrders(ProductCode, parentOrderId: ParentOrderAcceptanceId);
            if (resp.IsError)
            {
                DebugEx.Trace();
                return false;
            }
            var orders = resp.GetResult();

            return true;
        }

        bool ConfirmParentOrder(BitFlyerClient client)
        {
            //var resp = client.GetParentOrders(ProductCode, )
            return true;
        }

        bool ConfirmChildOrders(BitFlyerClient client)
        {
            var resp = client.GetChildOrders(ProductCode, parentOrderId: ParentOrderAcceptanceId);
            if (resp.IsError)
            {
                return false;
            }

            var orders = resp.GetResult();
            if (orders.Length == 0)
            {
                return false;
            }

            _childOrders = orders;
            return true;
        }

        public bool Cancel(BitFlyerClient client)
        {
            var resp = client.CancelParentOrder(ProductCode, parentOrderAcceptanceId: ParentOrderAcceptanceId);
            return !resp.IsError;
        }
    }
}
