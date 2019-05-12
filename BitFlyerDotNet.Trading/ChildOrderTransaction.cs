//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
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
    class ChildOrderTransaction : IChildOrderTransaction
    {
        // IChildOrder
        public BfProductCode ProductCode { get { return _request.ProductCode; } }
        public BfOrderType OrderType { get { return _request.OrderType; } }
        public BfTradeSide Side { get { return _request.Side; } }
        public decimal OrderSize { get { return _request.Size; } }
        public decimal OrderPrice { get { return _request.Price; } }
        public decimal StopTriggerPrice { get { throw new NotSupportedException(); } }
        public decimal TrailingStopPriceOffset { get { throw new NotSupportedException(); } }

        // IOrderTransaction
        public BfOrderState OrderStatus { get { return (_order == null) ? BfOrderState.Unknown : _order.ChildOrderState; } }
        public DateTime OrderDate { get { return (_order == null) ? default(DateTime) : _order.ChildOrderDate; } }
        public DateTime OrderCreatedTime { get; private set; }
        public DateTime OrderRequestedTime { get; private set; }
        public DateTime OrderAcceptedTime { get; private set; }
        public decimal ReferencePrice { get { return _ticker.MidPrice; } }

        // IChildOrderTransaction
        public string ChildOrderAcceptanceId { get { return _response.GetResult().ChildOrderAcceptanceId; } }
        public string ChildOrderId { get { return (_order == null) ? string.Empty : _order.ChildOrderId; } }
        public decimal ExecutedPrice { get { return _execs.IsEmpty() ? decimal.Zero : _execs.Sum(e => e.Price * e.Size) / _execs.Sum(e => e.Size); } }
        public decimal ExecutedSize { get { return _execs.Sum(e => e.Size); } }
        public DateTime ExecutedTime { get { return _execs.Select(e => e.ExecutedTime).DefaultIfEmpty().Max(); } }

        public int MinuteToExpire { get { return _request.MinuteToExpire; } }
        public BfTimeInForce TimeInForce { get { return _request.TimeInForce; } }

        public bool IsError { get { return _response != null && _response.IsError; } }
        public bool IsExecuted { get { return _status == OrderTransactionState.Executed; } }
        public bool IsCompleted { get { return _status.IsCompleted(); } }
        public bool IsCancelable { get { return _status.IsCancelable(); } }

        public object Tag { get; set; }

        // Properties
        ITradingAccount _account;
        BfChildOrderRequest _request;
        BitFlyerResponse<BfChildOrderResponse> _response;
        BfChildOrder _order;
        List<IBfExecution> _execs = new List<IBfExecution>();
        CompositeDisposable _disposables = new CompositeDisposable();
        BfTicker _ticker;

        // Manage order status
        ReaderWriterLockSlim _statusLock = new ReaderWriterLockSlim();
        public event OrderTransactionStatusChangedCallback StatusChanged;
        OrderTransactionState _status;
        public OrderTransactionState TransactionStatus
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

        void UpdateStatus(OrderTransactionState status)
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

        Timer _orderConfirmPollingTimer;
        public TimeSpan OrderConfirmPollingInterval { get; set; } = TimeSpan.FromSeconds(3);
        public static bool MonitorExecution { get; set; } = true;

        public ChildOrderTransaction(ITradingAccount account, BfOrderType orderType, BfTradeSide side, decimal size, decimal price)
        {
            DebugEx.EnterMethod();
            _account = account;
            _ticker = _account.Ticker;

            OrderCreatedTime = _account.ServerTime;
            StatusChanged += _account.OnOrderStatusChanged;

            _request = new BfChildOrderRequest
            {
                ProductCode = _account.ProductCode,
                OrderType = orderType,
                Side = side,
                Size = size,
                Price = price,
                MinuteToExpire = _account.MinuteToExpire,
                TimeInForce = _account.TimeInForce,
            };

            UpdateStatus(OrderTransactionState.Created);
        }

        public ChildOrderTransaction(ITradingAccount account, BfOrderType orderType, BfTradeSide side, decimal size)
        {
            DebugEx.EnterMethod();
            _account = account;
            _ticker = _account.Ticker;

            OrderCreatedTime = _account.ServerTime;
            StatusChanged += _account.OnOrderStatusChanged;

            _request = new BfChildOrderRequest
            {
                ProductCode = _account.ProductCode,
                OrderType = orderType,
                Side = side,
                Size = size,
                MinuteToExpire = _account.MinuteToExpire,
                TimeInForce = _account.TimeInForce,
            };

            UpdateStatus(OrderTransactionState.Created);
        }

        public bool Send()
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
                    UpdateStatus(OrderTransactionState.Ordering);
                    _response = _account.Client.SendChildOrder(_request);
                    if (_response.IsError)
                    {
                        _disposables.Dispose();
                        UpdateStatus(OrderTransactionState.OrderFailed);
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
                    UpdateStatus(OrderTransactionState.OrderAccepted);
                    _orderConfirmPollingTimer = new Timer(OnConfirmPollingTimerExpired, this, TimeSpan.Zero, OrderConfirmPollingInterval).AddTo(_disposables);
                    DebugEx.Trace();
                    return true;
                }
                catch
                {
                    DebugEx.Trace();
                    _disposables.Dispose();
                    UpdateStatus(OrderTransactionState.OrderFailed);
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
                UpdateStatus(OrderTransactionState.Executing);
            }
            else
            {
                DebugEx.Trace();
                switch (_status)
                {
                    case OrderTransactionState.Canceling:
                    case OrderTransactionState.CancelAccepted:
                        UpdateStatus(OrderTransactionState.CancelIgnored);
                        break;

                    default:
                        UpdateStatus(OrderTransactionState.Executed);
                        break;
                }
            }
            DebugEx.ExitMethod();
        }

        void OnConfirmPollingTimerExpired(object _)
        {
            DebugEx.EnterMethod();
            _orderConfirmPollingTimer.Change(Timeout.Infinite, Timeout.Infinite); // stop timer

            if (!Confirm() || !IsCompleted)
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
        public bool Cancel()
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
                    if (_status == OrderTransactionState.OrderFailed)
                    {
                        DebugEx.Trace();
                        UpdateStatus(OrderTransactionState.Canceled);
                        return true;
                    }

                    // During cancelable, monitoring timer is running
                    UpdateStatus(OrderTransactionState.Canceling);
                    var resp = _account.Client.CancelChildOrder(ProductCode, childOrderAcceptanceId: ChildOrderAcceptanceId);
                    UpdateStatus(resp.IsError ? OrderTransactionState.CancelFailed : OrderTransactionState.CancelAccepted);
                    DebugEx.Trace();
                    return !resp.IsError;
                }
                catch
                {
                    DebugEx.Trace();
                    UpdateStatus(OrderTransactionState.CancelFailed);
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

        public bool Confirm()
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
                var resp = _account.Client.GetChildOrders(ProductCode, childOrderAcceptanceId: ChildOrderAcceptanceId);
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
                    _order = orders[0];
                    var respExec = _account.Client.GetPrivateExecutions(ProductCode, childOrderAcceptanceId: ChildOrderAcceptanceId);
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
                    switch (OrderStatus)
                    {
                        case BfOrderState.Unknown: // Primitive stauts element was empty
                            DebugEx.Trace();
                            if (_status == OrderTransactionState.CancelAccepted)
                            {
                                DebugEx.Trace();
                                UpdateStatus(OrderTransactionState.Canceled);
                            }
                            return true;

                        case BfOrderState.Active:
                            DebugEx.Trace();
                            if (_execs.IsEmpty())
                            {
                                DebugEx.Trace();
                                UpdateStatus(OrderTransactionState.OrderConfirmed);
                                return true;
                            }
                            UpdateStatus((OrderSize > ExecutedSize) ? OrderTransactionState.Executing : OrderTransactionState.Executed);
                            break;

                        case BfOrderState.Completed:
                            DebugEx.Trace();
                            switch (_status)
                            {
                                case OrderTransactionState.Canceling:
                                case OrderTransactionState.CancelAccepted:
                                    DebugEx.Trace();
                                    UpdateStatus(OrderTransactionState.CancelIgnored);
                                    break;

                                default:
                                    DebugEx.Trace();
                                    UpdateStatus(OrderTransactionState.Executed);
                                    break;
                            }
                            return true;

                        case BfOrderState.Canceled:
                            DebugEx.Trace();
                            UpdateStatus(OrderTransactionState.Canceled);
                            return true;

                        case BfOrderState.Expired:
                            DebugEx.Trace();
                            UpdateStatus(OrderTransactionState.Expired);
                            return true;

                        case BfOrderState.Rejected:
                            DebugEx.Trace();
                            UpdateStatus(OrderTransactionState.Rejected);
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
