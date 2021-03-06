﻿//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Reactive.Disposables;

namespace BitFlyerDotNet.LightningApi
{
    public class WebSocketErrorStatus
    {
        public SocketError SocketError { get; set; } = SocketError.Success;
        public string Message { get; set; }
    }

    public class RealtimeSourceFactory : IDisposable
    {
        const string EndpointUrl = "wss://ws.lightstream.bitflyer.com/json-rpc";
        public static int WebSocketReconnectionIntervalMs
        {
            get { return WebSocketChannels.WebSocketReconnectionIntervalMs; }
            set { WebSocketChannels.WebSocketReconnectionIntervalMs = value; }
        }

        public long TotalReceivedMessageChars => Channels.TotalReceivedMessageChars;

        public event Action<WebSocketErrorStatus> Error
        {
            add { Channels.Error += value; }
            remove { Channels.Error -= value; }
        }

        public event Action ConnectionSuspended
        {
            add { Channels.Suspended += value; }
            remove { Channels.Suspended -= value; }
        }

        public event Action ConnectionResumed
        {
            add { Channels.Resumed += value; }
            remove { Channels.Resumed -= value; }
        }

        public static System.Reactive.Concurrency.IScheduler Scheduler { get; set; } = System.Reactive.Concurrency.Scheduler.Default;

        internal WebSocketChannels Channels { get; private set; }
        public event Action<string> MessageSent;
        public event Action<string, object> MessageReceived;

        void OnMessageSent(string json) => MessageSent?.Invoke(json);
        void OnMessageReceived(string json, object message) => MessageReceived?.Invoke(json, message);

        CompositeDisposable _disposables = new ();
        public static RealtimeSourceFactory Singleton { get; } = new();

        /// <summary>
        /// Create public realtime source
        /// </summary>
        public RealtimeSourceFactory()
        {
            Channels = new WebSocketChannels(EndpointUrl).AddTo(_disposables);
            Channels.MessageSent = OnMessageSent;
            Channels.MessageReceived = OnMessageReceived;
        }

        /// <summary>
        /// Create pricate realtime source
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="apiSecret"></param>
        public RealtimeSourceFactory(string apiKey, string apiSecret)
        {
            Channels = new WebSocketChannels(EndpointUrl, apiKey, apiSecret).AddTo(_disposables);
            Channels.MessageSent = OnMessageSent;
            Channels.MessageReceived = OnMessageReceived;
        }

        public bool Authenticate(string apiKey, string apiSecret)
        {
            return Channels.Authenticate(apiKey, apiSecret);
        }

        public void Dispose()
        {
            Log.Trace($"{nameof(RealtimeSourceFactory)} disposing...");
            _disposables.Dispose();
            Log.Trace($"{nameof(RealtimeSourceFactory)} diposed");
        }

        // Convert BfProdcutCode (inc. futures) to native product codes
        Dictionary<BfProductCode, string> _availableMarkets = new ();
        async Task GetAvailableMarkets()
        {
            using (var client = new BitFlyerClient())
            {
                _availableMarkets.Clear();
                foreach (var market in await client.GetAvailableMarketsAsync(CancellationToken.None))
                {
                    _availableMarkets[market.ProductCode] = market.Symbol;
                }
            }
        }

        bool _opened = false;
        public async Task<bool> TryOpenAsync()
        {
            if (_opened)
            {
                return false;
            }

            Channels.Resumed += async () => await GetAvailableMarkets(); // To refresh markets when is resumed
            await Channels.TryOpenAsync();
            await GetAvailableMarkets();
            _opened = true;
            return true;
        }

        void OnSourceClosed()
        {
#if AUTOCLOSE
            var openedCount = 0;
            openedCount += _executionColdSources.Count;
            openedCount += _tickSources.Count;
            openedCount += _orderBookSnapshotSources.Count;
            if (_childOrderEventSource != null)
            {
                openedCount++;
            }
            if (_parentOrderEventSource != null)
            {
                openedCount++;
            }

            if (openedCount == 0)
            {
                Channels.Close();
                _opened = false;
            }
#endif
        }

        ConcurrentDictionary<string, IObservable<BfaExecution>> _executionSources = new ();
        public IObservable<BfaExecution> GetExecutionSource(BfProductCode productCode, bool startPending = false)
        {
            var symbol = _availableMarkets[productCode];
            var result = _executionSources.GetOrAdd(symbol, _ =>
            {
                var source = new RealtimeExecutionSource(Channels, symbol, s =>
                {
                    _executionSources.TryRemove(s.ProductCode, out var _);
                    OnSourceClosed();
                });
                Channels.RegisterSource(source);
                if (startPending)
                {
                    return source.ObserveOn(Scheduler).SkipWhile(tick => tick.ExecutionId == 0).Publish();
                }
                else
                {
                    return source.ObserveOn(Scheduler).SkipWhile(tick => tick.ExecutionId == 0).Publish().RefCount();
                }
            });

            return result;
        }

        public void StartExecutionSource(BfProductCode productCode)
        {
            if (_executionSources.TryGetValue(_availableMarkets[productCode], out var source))
            {
                if (source is IConnectableObservable<BfaExecution> publishSource)
                {
                    publishSource.Connect().AddTo(_disposables);
                }
            }
        }

        public void StartAllExecutionSources()
        {
            foreach (var source in _executionSources.Values)
            {
                if (source is IConnectableObservable<BfaExecution> publishSource)
                {
                    publishSource.Connect().AddTo(_disposables);
                }
            }
        }

        ConcurrentDictionary<string, IObservable<BfTicker>> _tickSources = new ();
        public IObservable<BfTicker> GetTickerSource(BfProductCode productCode)
        {
            var symbol = _availableMarkets[productCode];
            return _tickSources.GetOrAdd(symbol, _ => // Cause ArgumentException if key not found.
            {
                var source = new RealtimeTickerSource(Channels, symbol, s =>
                {
                    _tickSources.TryRemove(s.ProductCode, out var _);
                    OnSourceClosed();
                });
                Channels.RegisterSource(source);
                return source.ObserveOn(Scheduler).Publish().RefCount();
            });
        }

        ConcurrentDictionary<string, IObservable<BfOrderBook>> _orderBookSnapshotSources = new ();
        public IObservable<BfOrderBook> GetOrderBookSource(BfProductCode productCode)
        {
            var symbol = _availableMarkets[productCode];
            return _orderBookSnapshotSources.GetOrAdd(symbol, _ => // Cause ArgumentException if key not found.
            {
                var snapshot = new RealtimeBoardSnapshotSource(Channels, symbol);
                var update = new RealtimeBoardSource(Channels, symbol);
                Channels.RegisterSource(snapshot);
                Channels.RegisterSource(update);
                var source = new BfOrderBookStream(symbol, snapshot, update, s =>
                {
                    _orderBookSnapshotSources.TryRemove(s.ProductCode, out var _);
                    OnSourceClosed();
                });
                return source.ObserveOn(Scheduler).Publish().RefCount();
            });
        }

        IObservable<BfChildOrderEvent> _childOrderEventSource;
        /// <summary>
        /// Get child order event source
        /// <see href="https://scrapbox.io/BitFlyerDotNet/ChildOrderEvent">Online help</see>
        /// </summary>
        /// <returns></returns>
        public IObservable<BfChildOrderEvent> GetChildOrderEventsSource()
        {
            if (_childOrderEventSource != null)
            {
                return _childOrderEventSource;
            }

            var source = new RealtimeChildOrderEventsSource(Channels, s=>
            {    _childOrderEventSource = null;
                OnSourceClosed();
            });
            Channels.RegisterSource(source);
            _childOrderEventSource = source.ObserveOn(Scheduler).Publish().RefCount();
            return _childOrderEventSource;
        }

        IObservable<BfParentOrderEvent> _parentOrderEventSource;
        /// <summary>
        /// Get parent order event source
        /// <see href="https://scrapbox.io/BitFlyerDotNet/ParentOrderEvent">Online help</see>
        /// </summary>
        /// <returns></returns>
        public IObservable<BfParentOrderEvent> GetParentOrderEventsSource()
        {
            if (_parentOrderEventSource != null)
            {
                return _parentOrderEventSource;
            }

            var source = new RealtimeParentOrderEventsSource(Channels, s =>
            {
                _parentOrderEventSource = null;
                OnSourceClosed();
            });
            Channels.RegisterSource(source);
            _parentOrderEventSource = source.ObserveOn(Scheduler).Publish().RefCount();
            return _parentOrderEventSource;
        }
    }
}
