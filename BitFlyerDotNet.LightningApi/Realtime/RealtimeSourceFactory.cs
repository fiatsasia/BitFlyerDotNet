//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using Newtonsoft.Json;
using Financial.Extensions;

namespace BitFlyerDotNet.LightningApi
{
    public class WebSocketErrorStatus
    {
        public SocketError SocketError { get; set; } = SocketError.Success;
        public string Message { get; set; }
    }

    public class RealtimeSourceFactory : IDisposable
    {
        public event Action<WebSocketErrorStatus> Error
        {
            add { _channels.Error += value; }
            remove { _channels.Error -= value; }
        }

        CompositeDisposable _disposables = new CompositeDisposable();
        JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        WebSocketChannels _channels;
        BitFlyerClient _client;

        public RealtimeSourceFactory() : this(new BitFlyerClient()) { _disposables.Add(_client); }
        public RealtimeSourceFactory(BitFlyerClient client)
        {
            if (client == null)
            {
                throw new ArgumentException("can not be null", nameof(client));
            }
            _client = client;

            Initialize();
        }

        public void Initialize()
        {
            GetAvailableMarkets();

            _channels = new WebSocketChannels("wss://ws.lightstream.bitflyer.com/json-rpc").AddTo(_disposables);
            _channels.Opened += () => GetAvailableMarkets();
        }

        public void Dispose()
        {
            Debug.Print($"{nameof(RealtimeSourceFactory)}.Dispose");
            _disposables.Dispose();
            Debug.Print($"{nameof(RealtimeSourceFactory)}.Dispose exit");
        }

        // Convert BfProdcutCode (inc. futures) to native product codes
        Dictionary<BfProductCode, string> _availableMarkets = new Dictionary<BfProductCode, string>();
        public IEnumerable<BfProductCode> AvailableMarkets => _availableMarkets.Keys;
        public BfProductCode ParseProductCode(string productCode) => _availableMarkets.Where(kvs => kvs.Value == productCode).Select(kvs => kvs.Key).Single();
        void GetAvailableMarkets()
        {
            _availableMarkets.Clear();
            foreach (var market in _client.GetMarketsAll().SelectMany(e => e.GetResult()))
            {
                if (market.ProductCode.StartsWith("BTCJPY"))
                {
                    if (string.IsNullOrEmpty(market.Alias))
                    {
                        continue; // ******** BTCJPY future somtimes missing alias, skip it ********
                    }
                    _availableMarkets.Add((BfProductCode)Enum.Parse(typeof(BfProductCode), market.Alias.Replace("_", "")), market.ProductCode);
                }
                else
                {
                    _availableMarkets.Add((BfProductCode)Enum.Parse(typeof(BfProductCode), market.ProductCode.Replace("_", "")), market.ProductCode);
                }
            }
        }

        ConcurrentDictionary<string, IConnectableObservable<BfExecution>> _executionColdSources = new ConcurrentDictionary<string, IConnectableObservable<BfExecution>>();
        public IObservable<BfExecution> GetExecutionSource(BfProductCode productCode, bool coldStart = false)
        {
            _channels.Open();
            var symbol = _availableMarkets[productCode];
            var result = _executionColdSources.GetOrAdd(symbol, _ =>
            {
                var source = new RealtimeExecutionSource(_channels, _jsonSettings, symbol);
                _channels.RegisterSource(source);
                return source.ObserveOn(Scheduler.Default).SkipWhile(tick => tick.ExecutionId == 0).Publish();
            });

            return coldStart ? result : result.RefCount();
        }

        public void StartExecutionSource(BfProductCode productCode)
        {
            if (_executionColdSources.TryGetValue(_availableMarkets[productCode], out IConnectableObservable<BfExecution> source))
            {
                source.Connect().AddTo(_disposables);
            }
        }

        public void StartAllExecutionSources()
        {
            foreach (var source in _executionColdSources.Values)
            {
                source.Connect().AddTo(_disposables);
            }
        }

        ConcurrentDictionary<string, IObservable<BfTicker>> _tickSources = new ConcurrentDictionary<string, IObservable<BfTicker>>();
        public IObservable<BfTicker> GetTickerSource(BfProductCode productCode)
        {
            _channels.Open();
            var symbol = _availableMarkets[productCode];
            return _tickSources.GetOrAdd(symbol, _ =>
            {
                var source = new RealtimeTickerSource(_channels, _jsonSettings, symbol);
                _channels.RegisterSource(source);
                return source.ObserveOn(Scheduler.Default).Publish().RefCount();
            });
        }

#if true // Will be obsoleted
        ConcurrentDictionary<string, IObservable<BfBoard>> _boardSources = new ConcurrentDictionary<string, IObservable<BfBoard>>();
        [Obsolete("This API will be deprecated. Please use GetOrderBookSource() instead of this.")]
        public IObservable<BfBoard> GetBoardSource(BfProductCode productCode)
        {
            _channels.Open();
            var symbol = _availableMarkets[productCode];
            return _boardSources.GetOrAdd(symbol, _ =>
            {
                var source = new RealtimeBoardSource(_channels, _jsonSettings, symbol);
                _channels.RegisterSource(source);
                return source.ObserveOn(Scheduler.Default).Publish().RefCount();
            });
        }

        ConcurrentDictionary<string, IObservable<BfBoard>> _boardSnapshotSources = new ConcurrentDictionary<string, IObservable<BfBoard>>();
        [Obsolete("This API will be deprecated. Please use GetOrderBookSource() instead of this.")]
        public IObservable<BfBoard> GetBoardSnapshotSource(BfProductCode productCode)
        {
            _channels.Open();
            var symbol = _availableMarkets[productCode];
            return _boardSnapshotSources.GetOrAdd(symbol, _ =>
            {
                var source = new RealtimeBoardSnapshotSource(_channels, _jsonSettings, symbol);
                _channels.RegisterSource(source);
                return source.ObserveOn(Scheduler.Default).Publish().RefCount();
            });
        }
#endif

        ConcurrentDictionary<string, IObservable<BfOrderBook>> _orderBookSnapshotSources = new ConcurrentDictionary<string, IObservable<BfOrderBook>>();
        public IObservable<BfOrderBook> GetOrderBookSource(BfProductCode productCode)
        {
            _channels.Open();
            var symbol = _availableMarkets[productCode];
            return _orderBookSnapshotSources.GetOrAdd(symbol, _ =>
            {
                var snapshot = new RealtimeBoardSnapshotSource(_channels, _jsonSettings, symbol);
                var update = new RealtimeBoardSource(_channels, _jsonSettings, symbol);
                _channels.RegisterSource(snapshot);
                _channels.RegisterSource(update);
                var source = new BfOrderBookStream(snapshot, update);
                return source.ObserveOn(Scheduler.Default).Publish().RefCount();
            });
        }

        IObservable<BfChildOrderEvent> _childOrderEventSource;
        public IObservable<BfChildOrderEvent> GetChildOrderEventsSource(string key, string secret)
        {
            if (_childOrderEventSource != null)
            {
                return _childOrderEventSource;
            }

            _channels.Open();
            var source = new RealtimeChildOrderEventsSource(_channels, _jsonSettings, key, secret);
            _channels.RegisterSource(source);
            return source.ObserveOn(Scheduler.Default).Publish().RefCount();
        }

        IObservable<BfParentOrderEvent> _parentOrderEventSource;
        public IObservable<BfParentOrderEvent> GetParentOrderEventsSource(string key, string secret)
        {
            if (_parentOrderEventSource != null)
            {
                return _parentOrderEventSource;
            }

            _channels.Open();
            var source = new RealtimeParentOrderEventsSource(_channels, _jsonSettings, key, secret);
            _channels.RegisterSource(source);
            return source.ObserveOn(Scheduler.Default).Publish().RefCount();
        }
    }
}
