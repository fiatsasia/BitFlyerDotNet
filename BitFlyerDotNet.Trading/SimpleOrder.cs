//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Fiats.Utils;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    internal class SimpleOrder : IBfTradeOrder
    {
        ITradeAccount _account;
        BfParentOrderRequest _request;
        BitFlyerResponse<BfParentOrderResponse> _response;
        BfChildOrder _order;
        List<IBfExecution> _execs = new List<IBfExecution>();
        CompositeDisposable _disposables = new CompositeDisposable();
        BfTicker _ticker;

        // Properties
        public bool IsSimple { get { return true; } }
        public BfProductCode ProductCode { get { return _request.Paremters[0].ProductCode; } }
        public BfOrderType OrderType { get { return _request.Paremters[0].ConditionType; } }
        public BfTradeSide Side { get { return _request.Paremters[0].Side; } }
        public object Tag { get; set; }

        // Order informations
        public DateTime OrderCreatedTime { get; private set; }
        public DateTime OrderRequestedTime { get; private set; }
        public DateTime OrderAcceptedTime { get; private set; }
        public double OrderSize { get { return _request.Paremters[0].Size; } }
        public double OrderPrice { get { return _request.Paremters[0].Price == 0.0 ? double.NaN : _request.Paremters[0].Price; } }
        public double TriggerPrice { get { return 0.0; } }
        public double LimitOffset { get { return 0.0; } }
        public double ReferencePrice { get { return _ticker.MidPrice; } }

        // Executed informations
        public double ExecutedPrice { get { return _execs.IsEmpty() ? double.NaN : _execs.Sum(e => e.Price * e.Size) / _execs.Sum(e => e.Size); } }
        public double ExecutedSize { get { return _execs.Sum(e => e.Size); } }
        public DateTime ExecutedTime { get { return _execs.Select(e => e.ExecutedTime).LastOrDefault(); } }

        // Manage order status
        ReaderWriterLockSlim _statusLock = new ReaderWriterLockSlim();
        public event OrderStatusChangedCallback StatusChanged;
        BfTradeOrderState _status;
        public BfTradeOrderState Status
        {
            get
            {
                try
                {
                    _statusLock.TryEnterReadLock(Timeout.Infinite);
                    return _status;
                }
                finally
                {
                    _statusLock.ExitReadLock();
                }
            }
        }

        void UpdateStatus(BfTradeOrderState status)
        {
            _status = status;
            try
            {
                DebugEx.Trace("{0}", _status);
                StatusChanged?.Invoke(_status, this);
                if (_status.IsCompleted())
                {
                    _disposables.Dispose(); // Unsubscribe execuion, dispose timer
                }
            }
            catch
            {
                // Ignore exception
                DebugEx.Trace("{0}", _status);
            }
        }

        public bool IsExecuted { get { return _status == BfTradeOrderState.Executed; } }
        public bool IsCompleted { get { return _status.IsCompleted(); } }
        public bool IsCancelable { get { return _status.IsCancelable(); } }

        Timer _orderConfirmPollingTimer;
        public TimeSpan OrderConfirmPollingInterval { get; set; } = TimeSpan.FromSeconds(3);

        public bool IsError { get { return _response != null && _response.IsError; } }
        BfOrderState PrimitiveStatus { get { return (_order == null) ? BfOrderState.Unknown : _order.ChildOrderState; } }

        string ParentOrderAcceptanceId { get { return _response.GetResult().ParentOrderAcceptanceId; } }
        string ChildOrderAcceptanceId { get { return (_order == null) ? string.Empty : _order.ChildOrderAcceptanceId; } }
        string ChildOrderId { get { return (_order == null) ? string.Empty : _order.ChildOrderId; } }

        public static bool MonitorExecution { get; set; } = true;

        public SimpleOrder(ITradeAccount account, BfOrderType orderType, BfTradeSide side, double size, double price = 0.0, double triggerPrice = 0.0, double limitOffset = 0.0)
        {
            DebugEx.EnterMethod();
            _account = account;
            _ticker = _account.Ticker;

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

            UpdateStatus(BfTradeOrderState.Created);
        }

        /// <summary>
        /// Send simple order
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool Send(BitFlyerClient client)
        {
            DebugEx.EnterMethod();
            try
            {
                _statusLock.TryEnterUpgradeableReadLock(Timeout.Infinite);
                if (!_status.IsOrderable())
                {
                    DebugEx.Trace();
                    _response = null;
                    return false;
                }

                try
                {
                    DebugEx.Trace();
                    _statusLock.EnterWriteLock();
                    OrderRequestedTime = _account.ServerTime;
                    UpdateStatus(BfTradeOrderState.Ordering);
                    _response = client.SendParentOrder(_request);
                    if (_response.IsError)
                    {
                        _disposables.Dispose();
                        UpdateStatus(BfTradeOrderState.OrderFailed);
                        DebugEx.Trace();
                        return false;
                    }

                    // Start execution monitoring
                    if (MonitorExecution)
                    {
                        DebugEx.Trace();
                        _account.ExecutionSource.Subscribe(OnExecutionTicked).AddTo(_disposables);
                    }

                    OrderAcceptedTime = _account.ServerTime;
                    UpdateStatus(BfTradeOrderState.OrderAccepted);
                    _orderConfirmPollingTimer = new Timer(OnConfirmPollingTimerExired, client, TimeSpan.Zero, OrderConfirmPollingInterval).AddTo(_disposables);
                    DebugEx.Trace();
                    return true;
                }
                catch
                {
                    DebugEx.Trace();
                    _disposables.Dispose();
                    UpdateStatus(BfTradeOrderState.OrderFailed);
                    throw;
                }
                finally
                {
                    DebugEx.Trace();
                    _statusLock.ExitWriteLock();
                }
            }
            finally
            {
                DebugEx.Trace();
                _statusLock.ExitUpgradeableReadLock();
                DebugEx.ExitMethod();
            }
        }

        void OnExecutionTicked(IBfExecution exec)
        {
            if (exec.ChildOrderAcceptanceId != ChildOrderAcceptanceId)
            {
                return;
            }

            DebugEx.EnterMethod();
            _execs.Add(exec);
            if (ExecutedSize < OrderSize)
            {
                DebugEx.Trace();
                UpdateStatus(BfTradeOrderState.Executing);
            }
            else
            {
                DebugEx.Trace();
                switch (_status)
                {
                    case BfTradeOrderState.Canceling:
                    case BfTradeOrderState.CancelAccepted:
                        UpdateStatus(BfTradeOrderState.CancelIgnored);
                        break;

                    default:
                        UpdateStatus(BfTradeOrderState.Executed);
                        break;
                }
            }
            DebugEx.ExitMethod();
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

        /// <summary>
        /// Cancel simple order
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool Cancel(BitFlyerClient client)
        {
            DebugEx.EnterMethod();
            try
            {
                _statusLock.TryEnterUpgradeableReadLock(Timeout.Infinite);
                if (!_status.IsCancelable())
                {
                    DebugEx.Trace();
                    return false;
                }

                try
                {
                    DebugEx.Trace();
                    _statusLock.EnterWriteLock();
                    if (_status == BfTradeOrderState.OrderFailed)
                    {
                        DebugEx.Trace();
                        UpdateStatus(BfTradeOrderState.Canceled);
                        return true;
                    }

                    // During cancelable, monitoring timer is running
                    UpdateStatus(BfTradeOrderState.Canceling);
                    var resp = client.CancelParentOrder(ProductCode, parentOrderAcceptanceId: ParentOrderAcceptanceId);
                    UpdateStatus(resp.IsError ? BfTradeOrderState.CancelFailed : BfTradeOrderState.CancelAccepted);
                    DebugEx.Trace();
                    return !resp.IsError;
                }
                catch
                {
                    DebugEx.Trace();
                    UpdateStatus(BfTradeOrderState.CancelFailed);
                    throw;
                }
                finally
                {
                    _statusLock.ExitWriteLock();
                }
            }
            finally
            {
                _statusLock.ExitUpgradeableReadLock();
                DebugEx.ExitMethod();
            }
        }

        public bool Confirm(BitFlyerClient client)
        {
            DebugEx.Trace();
            try
            {
                _statusLock.TryEnterUpgradeableReadLock(Timeout.Infinite);
                if (_status.IsCompleted())
                {
                    DebugEx.Trace();
                    return true;
                }

                // Get order information
                var resp = client.GetChildOrders(ProductCode, parentOrderId: ParentOrderAcceptanceId);
                if (resp.IsError)
                {
                    DebugEx.Trace();
                    return false;
                }
                var orders = resp.GetResult();

                // Get execution information
                if (!orders.IsEmpty())
                {
                    DebugEx.Trace();
                    _order = orders.First();
                    var respExec = client.GetPrivateExecutions(ProductCode, childOrderAcceptanceId: ChildOrderAcceptanceId);
                    if (respExec.IsError)
                    {
                        DebugEx.Trace();
                        return false;
                    }
                    _execs.Clear();
                    _execs.AddRange(respExec.GetResult());
                }

                try
                {
                    DebugEx.Trace();
                    _statusLock.EnterWriteLock();
                    switch (PrimitiveStatus)
                    {
                        case BfOrderState.Unknown: // Primitive stauts element was empty
                            DebugEx.Trace();
                            if (_status == BfTradeOrderState.CancelAccepted)
                            {
                                DebugEx.Trace();
                                UpdateStatus(BfTradeOrderState.Canceled);
                            }
                            return true;

                        case BfOrderState.Active:
                            DebugEx.Trace();
                            if (_execs.IsEmpty())
                            {
                                DebugEx.Trace();
                                UpdateStatus(BfTradeOrderState.OrderConfirmed);
                                return true;
                            }
                            UpdateStatus((OrderSize > ExecutedSize) ? BfTradeOrderState.Executing : BfTradeOrderState.Executed);
                            break;

                        case BfOrderState.Completed:
                            DebugEx.Trace();
                            switch (_status)
                            {
                                case BfTradeOrderState.Canceling:
                                case BfTradeOrderState.CancelAccepted:
                                    DebugEx.Trace();
                                    UpdateStatus(BfTradeOrderState.CancelIgnored);
                                    break;

                                default:
                                    DebugEx.Trace();
                                    UpdateStatus(BfTradeOrderState.Executed);
                                    break;
                            }
                            return true;

                        case BfOrderState.Canceled:
                            DebugEx.Trace();
                            UpdateStatus(BfTradeOrderState.Canceled);
                            return true;

                        case BfOrderState.Expired:
                            DebugEx.Trace();
                            UpdateStatus(BfTradeOrderState.Expired);
                            return true;

                        case BfOrderState.Rejected:
                            DebugEx.Trace();
                            UpdateStatus(BfTradeOrderState.Rejected);
                            return true;
                    }
                }
                finally
                {
                    DebugEx.Trace();
                    _statusLock.ExitWriteLock();
                }
            }
            finally
            {
                DebugEx.Trace();
                _statusLock.ExitUpgradeableReadLock();
            }

            return true;
        }
    }
}
