//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
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
    public partial class TradeAccount : ITradeAccount
    {
        // Optional parameters
        public int MinuteToExpire { get; set; } = 0;
        public BfTimeInForce TimeInForce { get; set; } = BfTimeInForce.NotSpecified;
        public int OrderRetryMax { get; set; } = 3;
        public TimeSpan OrderRetryInterval { get; set; } = TimeSpan.FromSeconds(3);

        // Properties
        public BfProductCode ProductCode { get; private set; }
        BitFlyerClient _client;
        string[] _permissions;
        ConcurrentBag<BfPosition> _positions = new ConcurrentBag<BfPosition>();
        public IEnumerable<BfPosition> Positions { get { return _positions; } }
        CompositeDisposable _disposables = new CompositeDisposable();

        // Realtime resources
        public BfTicker Ticker { get; private set; }
        IBfExecution _execution;
        public DateTime ServerTime { get { return Ticker.Timestamp; } }
        public double AskPrice { get { return Ticker.BestAsk; } }
        public double BidPrice { get { return Ticker.BestBid; } }

        public event OrderStatusChangedCallback OrderStatusChanged;
        public event PositionStatusChangedCallback PositionStatusChanged;

        public IObservable<BfTicker> TickerSource { get; private set; }
        public IObservable<IBfExecution> ExecutionSource { get; private set; }

        public TradeAccount(BfProductCode productCode)
        {
            ProductCode = productCode;

            // FXBTCJPY の場合は、SFDがあるので、現物も同時に受ける必要がある。
            var factory = new BitFlyerRealtimeSourceFactory();
            TickerSource = factory.GetTickerSource(ProductCode);
            TickerSource.Subscribe(ticker => { Ticker = ticker; }).AddTo(_disposables);
            ExecutionSource = factory.GetExecutionSource(ProductCode);
            ExecutionSource.Subscribe(execution => { _execution = execution; }).AddTo(_disposables);
            factory.StartExecutionSource(ProductCode);
        }

        public void Login(string apiKey, string apiSecret)
        {
            _client = new BitFlyerClient(apiKey, apiSecret);

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                return;
            }

            // Check API permissions
            {
                var resp = _client.GetPermissions();
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
                var resp = _client.GetPositions(ProductCode);
                if (resp.IsError)
                {
                    throw new ApplicationException(resp.ErrorMessage);
                }

                resp.GetResult().ForEach(e => _positions.Add(e));
            }
        }

        public Task<bool> PlaceOrder(IBfTradeOrder order, int retryMax, TimeSpan retryInterval)
        {
            DebugEx.Trace();
            return Task.Run(() =>
            {
                DebugEx.Trace();
                for (var retry = 0; retry <= retryMax; retry++)
                {
                    DebugEx.Trace();
                    if (!order.Status.IsOrderable())
                    {
                        DebugEx.Trace();
                        return false;
                    }

                    if (!order.Send(_client))
                    {
                        DebugEx.Trace("Trying retry...");
                        if (!order.Status.IsOrderable())
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

        public Task<bool> PlaceOrder(IBfTradeOrder order)
        {
            DebugEx.Trace();
            return PlaceOrder(order, OrderRetryMax, OrderRetryInterval);
        }

        public void CancelOrder(IBfTradeOrder order)
        {
            order.Cancel(_client);
        }

        public void OnOrderStatusChanged(BfTradeOrderState status, IBfTradeOrder order)
        {
            OrderStatusChanged?.Invoke(status, order);

            if (status == BfTradeOrderState.Executing || status == BfTradeOrderState.Executed)
            {
                Task.Run(() =>
                {
                    DebugEx.Trace();
                    for (var retry = 0; retry <= OrderRetryMax; retry++)
                    {
                        DebugEx.Trace();
                        var resp = _client.GetPositions(ProductCode);
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
