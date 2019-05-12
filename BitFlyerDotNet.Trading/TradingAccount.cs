﻿//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Fiats.Utils;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public partial class TradingAccount : ITradingAccount
    {
        // Optional parameters
        public int MinuteToExpire { get; set; }
        public BfTimeInForce TimeInForce { get; set; } = BfTimeInForce.NotSpecified;
        public int OrderRetryMax { get; set; } = 3;
        public TimeSpan OrderRetryInterval { get; set; } = TimeSpan.FromSeconds(3);

        // Properties
        public BfProductCode ProductCode { get; private set; }
        public BitFlyerClient Client { get; private set; }
        string[] _permissions;
        ConcurrentBag<BfPosition> _positions = new ConcurrentBag<BfPosition>();
        public IReadOnlyCollection<BfPosition> Positions { get { return _positions; } }
        ConcurrentBag<ParentOrder> _parentOrders = new ConcurrentBag<ParentOrder>();
        public IReadOnlyCollection<ParentOrder> ActiveParentOrders { get { return _parentOrders; } }
        ConcurrentBag<ChildOrder> _childOrders = new ConcurrentBag<ChildOrder>();
        public IReadOnlyCollection<ChildOrder> ActiveChildOrders { get { return _childOrders; } }

        CompositeDisposable _disposables = new CompositeDisposable();

        // Realtime resources
        public BfTicker Ticker { get; private set; }
        IBfExecution _execution;
        public DateTime ServerTime { get { return Ticker.Timestamp; } }
        public decimal AskPrice { get { return Ticker.BestAsk; } }
        public decimal BidPrice { get { return Ticker.BestBid; } }

        public event OrderTransactionStatusChangedCallback OrderStatusChanged;
        public event PositionStatusChangedCallback PositionStatusChanged;

        public IObservable<BfTicker> TickerSource { get; private set; }
        public event TickerCallback TickerReceived;

        public IObservable<IBfExecution> ExecutionSource { get; private set; }
        public event ExecutionCallback ExecutionReceived;

        public TradingAccount(BfProductCode productCode)
        {
            ProductCode = productCode;

            // FXBTCJPY の場合は、SFDがあるので、現物も同時に受ける必要がある。
            var factory = new RealtimeSourceFactory();
            TickerSource = factory.GetTickerSource(ProductCode);
            TickerSource.Subscribe(ticker => { Ticker = ticker; TickerReceived?.Invoke(ticker); }).AddTo(_disposables);
            ExecutionSource = factory.GetExecutionSource(ProductCode, true);
            ExecutionSource.Subscribe(execution => { _execution = execution; ExecutionReceived?.Invoke(execution); }).AddTo(_disposables);
            factory.StartExecutionSource(ProductCode);
        }

        public void Login(string apiKey, string apiSecret)
        {
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                throw new ArgumentException("Invalid API ket or secret.");
            }

            if (Client != null)
            {
                throw new InvalidOperationException("Already logged-in");
            }
            Client = new BitFlyerClient(apiKey, apiSecret);

            // Check API permissions
            {
                var resp = Client.GetPermissions();
                if (resp.IsError)
                {
                    if (resp.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException("Invalid API key or API secret.");
                    }
                    else
                    {
                        throw new ApplicationException(resp.ErrorMessage);
                    }
                }
                _permissions = resp.GetResult();
                if (!_permissions.Where(e => e.Contains("v1/me/")).Any())
                {
                    throw new ApplicationException("Any of enabled private API permission is not found.");
                }
            }

            // Get positions
            {
                var resp = Client.GetPositions(ProductCode);
                if (resp.IsError)
                {
                    throw new ApplicationException(resp.ErrorMessage);
                }

                resp.GetResult().ForEach(e => _positions.Add(e));
            }

            // Get active orders
            {
                var resp = Client.GetParentOrders(ProductCode, BfOrderState.Active);
                if (resp.IsError)
                {
                    throw new ApplicationException(resp.ErrorMessage);
                }

                resp.GetResult().ForEach(e =>
                {
                    var resp2 = Client.GetParentOrder(ProductCode, e.ParentOrderId);
                    if (resp2.IsError)
                    {
                        throw new ApplicationException(resp.ErrorMessage);
                    }

                    _parentOrders.Add(new ParentOrder(ProductCode, resp2.GetResult()));
                });
            }
            {
                var resp = Client.GetChildOrders(ProductCode, BfOrderState.Active);
                if (resp.IsError)
                {
                    throw new ApplicationException(resp.ErrorMessage);
                }

                resp.GetResult().ForEach(e => _childOrders.Add(new ChildOrder(ProductCode, e)));
            }
        }

        public void Logout()
        {
            if (Client != null)
            {
                while (!_positions.IsEmpty) _positions.TryTake(out BfPosition pos);
                while (!_parentOrders.IsEmpty) _parentOrders.TryTake(out ParentOrder po);
                while (!_childOrders.IsEmpty) _childOrders.TryTake(out ChildOrder co);
                Client = null;
            }
        }

        public Task<bool> PlaceOrder(IOrderTransaction order, int retryMax, TimeSpan retryInterval)
        {
            DebugEx.Trace();
            return Task.Run(() =>
            {
                DebugEx.Trace();
                for (var retry = 0; retry <= retryMax; retry++)
                {
                    DebugEx.Trace();
                    if (!order.TransactionStatus.IsOrderable())
                    {
                        DebugEx.Trace();
                        return false;
                    }

                    if (!order.Send())
                    {
                        DebugEx.Trace("Trying retry...");
                        if (!order.TransactionStatus.IsOrderable())
                        {
                            DebugEx.Trace();
                            return false;
                        }

                        Thread.Sleep(retryInterval.Milliseconds);
                        continue;
                    }
                    break;
                }

                DebugEx.Trace();
                return order.IsError;
            });
        }

        public Task<bool> PlaceOrder(IOrderTransaction order)
        {
            DebugEx.Trace();
            return PlaceOrder(order, OrderRetryMax, OrderRetryInterval);
        }

        public void CancelOrder(IOrderTransaction order)
        {
            order.Cancel();
        }

        public void OnOrderStatusChanged(OrderTransactionState status, IOrderTransaction order)
        {
            OrderStatusChanged?.Invoke(status, order);

            if (status == OrderTransactionState.Executing || status == OrderTransactionState.Executed)
            {
                Task.Run(() =>
                {
                    DebugEx.Trace();
                    for (var retry = 0; retry <= OrderRetryMax; retry++)
                    {
                        DebugEx.Trace();
                        var resp = Client.GetPositions(ProductCode);
                        if (resp.IsError)
                        {
                            DebugEx.Trace("Trying retry...");
                            Thread.Sleep(OrderRetryInterval.Milliseconds);
                            continue;
                        }

                        // Notify opened positions
                        resp.GetResult().Except(_positions).ForEach(e => PositionStatusChanged?.Invoke(e, true));

                        // Notify closed positions
                        _positions.Except(resp.GetResult()).ForEach(e => PositionStatusChanged?.Invoke(e, false));

                        var positions = new ConcurrentBag<BfPosition>(resp.GetResult());
                        Interlocked.Exchange(ref _positions, positions);
                        break;
                    }
                });
            }
        }
    }
}
