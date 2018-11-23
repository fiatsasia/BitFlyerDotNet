//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Reactive.Disposables;
using Fiats.Utils;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    class ParentOrderTransaction : IParentOrderTransaction
    {
        // IParentOrder
        public BfProductCode ProductCode { get { return _request.Paremters[0].ProductCode; } }
        public virtual BfOrderType OrderType { get { return _request.OrderMethod != BfOrderType.Simple ? _request.OrderMethod : _request.Paremters[0].ConditionType; } }
        public IChildOrder[] ChildOrders { get; }

        // IOrderTransaction
        public DateTime OrderDate { get { return (_parentOrder == null) ? default(DateTime) : _parentOrder.ParentOrderDate; } }
        public DateTime OrderCreatedTime { get; private set; }
        public DateTime OrderRequestedTime { get; private set; }
        public DateTime OrderAcceptedTime { get; private set; }
        public double ReferencePrice { get { return _ticker.MidPrice; } }
        public BfOrderState OrderStatus { get { return (_parentOrder == null) ? BfOrderState.Unknown : _parentOrder.ParentOrderState; } }
        public int MinuteToExpire { get { return _request.MinuteToExpire; } }
        public BfTimeInForce TimeInForce { get { return _request.TimeInForce; } }
        public DateTime ExecutedTime { get { return Executions.Select(e => e.ExecutedTime).DefaultIfEmpty().Max(); } }
        public bool IsError { get { return _response.IsError; } }
        public bool IsExecuted { get { return _status == OrderTransactionState.Executed; } }
        public bool IsCompleted { get { return _status.IsCompleted(); } }
        public bool IsCancelable { get { return _status.IsCancelable(); } }
        public object Tag { get; set; }

        // Manage order status
        protected ReaderWriterLockSlim _statusLock = new ReaderWriterLockSlim();
        public event OrderTransactionStatusChangedCallback StatusChanged;
        protected OrderTransactionState _status;
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

        protected void UpdateStatus(OrderTransactionState status)
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


        // Interfaces
        public string ParentOrderAcceptanceId { get { return _response.GetResult().ParentOrderAcceptanceId; } }
        public string ParentOrderId { get { return _parentOrderDetail.ParentOrderId; } }
        public List<IBfExecution> Executions { get; private set; } = new List<IBfExecution>();

        protected ITradingAccount _account;
        protected BfParentOrderRequest _request;
        BitFlyerResponse<BfParentOrderResponse> _response;
        BfParentOrderDetail _parentOrderDetail;
        BfParentOrder _parentOrder;
        protected BfChildOrder[] _childOrders = new BfChildOrder[0];
        CompositeDisposable _disposables = new CompositeDisposable();
        BfTicker _ticker;

        // Prooperties
        public virtual BfTradeSide Side { get { return _parentOrder.Side; } }
        public virtual double OrderSize { get { return _parentOrder.Size; } }
        public virtual double OrderPrice { get { return _parentOrder.Price; } }

        public double ExecutedPrice { get; }
        public double ExecutedSize { get; }


        Timer _orderConfirmPollingTimer;
        public TimeSpan OrderConfirmPollingInterval { get; set; } = TimeSpan.FromSeconds(3);

        public ParentOrderTransaction(ITradingAccount account, BfOrderType method, IChildOrder[] orders)
        {
            _account = account;
            _ticker = _account.Ticker;
            ChildOrders = orders;
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
                _request.Paremters.Add(new BfParentOrderRequestParameter
                {
                    ProductCode = order.ProductCode,
                    ConditionType = order.OrderType,
                    Side = order.Side,
                    Size = order.OrderSize,
                    Price = order.OrderPrice,
                    TriggerPrice = order.StopTriggerPrice,
                    Offset = order.TrailingStopPriceOffset,
                });
            }
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
                    _response = _account.Client.SendParentOrder(_request);
                    if (_response.IsError)
                    {
                        _disposables.Dispose();
                        UpdateStatus(OrderTransactionState.OrderFailed);
                        DebugEx.Trace();
                        return false;
                    }

                    OrderAcceptedTime = _account.ServerTime;
                    UpdateStatus(OrderTransactionState.OrderAccepted);
                    _orderConfirmPollingTimer = new Timer(OnConfirmPollingTimerExired, this, TimeSpan.Zero, OrderConfirmPollingInterval).AddTo(_disposables);
                    DebugEx.Trace();
                    return true;
                }
                catch (Exception ex)
                {
                    DebugEx.Trace(ex.Message);
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

        void OnConfirmPollingTimerExired(object state)
        {
            DebugEx.EnterMethod();
            var client = state as BitFlyerClient;
            _orderConfirmPollingTimer.Change(Timeout.Infinite, Timeout.Infinite); // stop timer

            if (!Confirm() || !IsCompleted)
            {
                _orderConfirmPollingTimer.Change(OrderConfirmPollingInterval, OrderConfirmPollingInterval); // restart timer
                DebugEx.ExitMethod();
                return;
            }
            DebugEx.ExitMethod();
        }

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
                    var resp = _account.Client.CancelParentOrder(ProductCode, parentOrderAcceptanceId: ParentOrderAcceptanceId);
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

        public virtual bool Confirm()
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

                // Get parent order detail information
                if (_parentOrderDetail == null)
                {
                    var resp = _account.Client.GetParentOrder(ProductCode, parentOrderAcceptanceId: ParentOrderAcceptanceId);
                    if (resp.IsErrorOrEmpty)
                    {
                        return false;
                    }
                    _parentOrderDetail = resp.GetResult();
                }

                // Get parent order infomation to get parent order status by paging ID
                {
                    var resp = _account.Client.GetParentOrders(ProductCode, count: 1, before: _parentOrderDetail.PagingId + 1);
                    if (resp.IsErrorOrEmpty)
                    {
                        return false;
                    }

                    var orders = resp.GetResult();
                    if (orders[0].ParentOrderAcceptanceId != ParentOrderAcceptanceId)
                    {
                        return false; // Not registered yet
                    }
                    _parentOrder = orders[0];
                }

                // Get child orders
                {
                    var resp = _account.Client.GetChildOrders(ProductCode, parentOrderId: ParentOrderId); // parent order acceptance ID is not allowed.
                    if (!resp.IsError)
                    {
                        _childOrders = resp.GetResult();
                    }
                }

                // Get child order executions
                Executions.Clear();
                foreach (var childOrder in _childOrders)
                {
                    var resp = _account.Client.GetPrivateExecutions(ProductCode, childOrderId: childOrder.ChildOrderId);
                    if (!resp.IsErrorOrEmpty)
                    {
                        Executions.AddRange(resp.GetResult());
                    }
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
                            UpdateStatus(OrderTransactionState.OrderConfirmed);
                            return true;

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
