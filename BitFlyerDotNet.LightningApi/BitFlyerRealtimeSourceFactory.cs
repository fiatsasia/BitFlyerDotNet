//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PubNubMessaging.Core;
using WebSocket4Net;
using Fiats.Utils;

namespace BitFlyerDotNet.LightningApi
{
    public enum BfRealtimeSourceKind
    {
        PubNub,
        WebSocket,
    }

    public class BitFlyerRealtimeSourceFactory : IDisposable
    {
        CompositeDisposable _disposables = new CompositeDisposable();

        BfRealtimeSourceKind _sourceKind;
        Pubnub _pubnub;
        WebSocket _webSocket;
        AutoResetEvent _openedEvent = new AutoResetEvent(false);
        JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        ConcurrentDictionary<string, IRealtimeSource> _webSocketSources = new ConcurrentDictionary<string, IRealtimeSource>();
        Timer _wsReconnectionTimer;
        const int WebSocketReconnectionIntervalMs = 3000;

        BitFlyerClient _client = new BitFlyerClient();
        Dictionary<string, string> _productCodeAliases = new Dictionary<string, string>();

        public BitFlyerRealtimeSourceFactory(BfRealtimeSourceKind sourceKind = BfRealtimeSourceKind.PubNub)
        {
            _sourceKind = sourceKind;

            // Convert from future product code alias to real product code
            var resp = _client.GetMarkets();
            if (resp.IsError)
            {
                if (resp.Exception != null)
                {
                    throw resp.Exception;
                }
                else
                {
                    throw new InvalidOperationException(resp.ErrorMessage);
                }
            }
            foreach (var market in resp.GetResult())
            {
                if (!string.IsNullOrEmpty(market.Alias))
                {
                    _productCodeAliases[market.Alias] = market.ProductCode;
                }
                else
                {
                    _productCodeAliases[market.ProductCode] = market.ProductCode;
                }
            }

            switch (_sourceKind)
            {
                case BfRealtimeSourceKind.PubNub:
                    _pubnub = new Pubnub("", "sub-c-52a9ab50-291b-11e5-baaa-0619f8945a4f");
                    break;

                case BfRealtimeSourceKind.WebSocket:
                    {
                        _webSocket = new WebSocket("wss://ws.lightstream.bitflyer.com/json-rpc");
                        _webSocket.Security.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                        _wsReconnectionTimer = new Timer((_) =>
                        {
                            _wsReconnectionTimer.Change(Timeout.Infinite, Timeout.Infinite); // stop
                            Debug.WriteLine("{0} WebSocket is reopening connection... state={1}", DateTime.Now, _webSocket.State);
                            lock (_webSocket)
                            {
                                switch (_webSocket.State)
                                {
                                    case WebSocketState.None:
                                    case WebSocketState.Closed:
                                        try
                                        {
                                            _openedEvent.Reset();
                                            _webSocket.Close();
                                            _webSocket.Open();
                                            _openedEvent.WaitOne(10000);
                                            _webSocketSources.Values.ForEach(source => { source.Subscribe(); });
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine(ex.Message);
                                            _wsReconnectionTimer.Change(WebSocketReconnectionIntervalMs, Timeout.Infinite); // restart
                                        }
                                        break;

                                    default:
                                        _wsReconnectionTimer.Change(WebSocketReconnectionIntervalMs, Timeout.Infinite); // restart
                                        break;
                                }
                            }
                        });
                            
                        _openedEvent.Reset();
                        _webSocket.Opened += (_, __) => { _openedEvent.Set(); };
                        _webSocket.MessageReceived += (_, e) =>
                        {
                            var subscriptionResult = JObject.Parse(e.Message)["params"];
                            var channel = subscriptionResult["channel"].Value<string>();
                            _webSocketSources[channel].OnWebSocketSubscribe(subscriptionResult["message"]);
                        };
                        _webSocket.Closed += (_, e) =>
                        {
                            Debug.WriteLine("{0} WebSocket connection closed.", DateTime.Now);
                        };
                        _webSocket.Error += (_, e) =>
                        {
                            if (e.Exception is IOException) // Server disconnect during daily maintenance.
                            {
                                var socketEx = e.Exception.InnerException as System.Net.Sockets.SocketException;
                                if (socketEx != null)
                                {
                                    Debug.WriteLine("{0} WebSocket socket error({1})", DateTime.Now, socketEx.SocketErrorCode);
                                    switch (socketEx.SocketErrorCode)
                                    {
                                        case System.Net.Sockets.SocketError.ConnectionReset:
                                            Debug.WriteLine("{0} WebSocket connection reset. Probably server is not active.", DateTime.Now);
                                            throw socketEx;

                                        default:
                                            Debug.WriteLine("{0} WebSocket unpredictable error.", DateTime.Now);
                                            throw socketEx;
                                    }
                                }

                                Debug.WriteLine("{0} WebSocket caused IOException({1}). Will be closed and reopening...", DateTime.Now, e.Exception.Message);
                                _wsReconnectionTimer.Change(0, Timeout.Infinite);
                            }
                            else
                            {
                                throw e.Exception;
                            }
                        };
                        _webSocket.Open();
                        _openedEvent.WaitOne(10000);
                    }
                    break;
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        ConcurrentDictionary<BfProductCode, IConnectableObservable<BfExecution>> _executionSources = new ConcurrentDictionary<BfProductCode, IConnectableObservable<BfExecution>>();
        public IObservable<BfExecution> GetExecutionSource(BfProductCode productCode)
        {
            return _executionSources.GetOrAdd(productCode, _ =>
            {
                var realProductCode = _productCodeAliases[productCode.ToEnumString()];
                var source = default(RealtimeExecutionSource);
                switch (_sourceKind)
                {
                    case BfRealtimeSourceKind.PubNub:
                        source = new RealtimeExecutionSource(_pubnub, _jsonSettings, realProductCode);
                        break;

                    case BfRealtimeSourceKind.WebSocket:
                        source = new RealtimeExecutionSource(_webSocket, _jsonSettings, realProductCode);
                        _webSocketSources[source.Channel] = source;
                        break;
                }
                return source.SkipWhile(tick => tick.ExecutionId == 0).Publish();
            });
        }

        public void StartExecutionSource(BfProductCode productCode)
        {
            IConnectableObservable<BfExecution> source;
            if (_executionSources.TryGetValue(productCode, out source))
            {
                source.Connect().AddTo(_disposables);
            }
        }

        public void StartAllExecutionSources()
        {
            foreach (var source in _executionSources.Values)
            {
                source.Connect().AddTo(_disposables);
            }
        }

        ConcurrentDictionary<BfProductCode, IObservable<BfTicker>> _tickSources = new ConcurrentDictionary<BfProductCode, IObservable<BfTicker>>();
        public IObservable<BfTicker> GetTickerSource(BfProductCode productCode)
        {
            return _tickSources.GetOrAdd(productCode, _ =>
            {
                var realProductCode = _productCodeAliases[productCode.ToEnumString()];
                var source = default(RealtimeTickerSource);
                switch (_sourceKind)
                {
                    case BfRealtimeSourceKind.PubNub:
                        source = new RealtimeTickerSource(_pubnub, _jsonSettings, realProductCode);
                        break;

                    case BfRealtimeSourceKind.WebSocket:
                        source = new RealtimeTickerSource(_webSocket, _jsonSettings, realProductCode);
                        _webSocketSources[source.Channel] = source;
                        break;
                }
                return source.Publish().RefCount();
            });
        }

        ConcurrentDictionary<BfProductCode, IObservable<BfBoard>> _boardSources = new ConcurrentDictionary<BfProductCode, IObservable<BfBoard>>();
        public IObservable<BfBoard> GetBoardSource(BfProductCode productCode)
        {
            return _boardSources.GetOrAdd(productCode, _ =>
            {
                var realProductCode = _productCodeAliases[productCode.ToEnumString()];
                var source = default(RealtimeBoardSource);
                switch (_sourceKind)
                {
                    case BfRealtimeSourceKind.PubNub:
                        source = new RealtimeBoardSource(_pubnub, _jsonSettings, realProductCode);
                        break;

                    case BfRealtimeSourceKind.WebSocket:
                        source = new RealtimeBoardSource(_webSocket, _jsonSettings, realProductCode);
                        _webSocketSources[source.Channel] = source;
                        break;
                }
                return source.Publish().RefCount();
            });
        }

        ConcurrentDictionary<BfProductCode, IObservable<BfBoard>> _boardSnapshotSources = new ConcurrentDictionary<BfProductCode, IObservable<BfBoard>>();
        public IObservable<BfBoard> GetBoardSnapshotSource(BfProductCode productCode)
        {
            return _boardSnapshotSources.GetOrAdd(productCode, _ =>
            {
                var realProductCode = _productCodeAliases[productCode.ToEnumString()];
                var source = default(RealtimeBoardSnapshotSource);
                switch (_sourceKind)
                {
                    case BfRealtimeSourceKind.PubNub:
                        source = new RealtimeBoardSnapshotSource(_pubnub, _jsonSettings, realProductCode);
                        break;

                    case BfRealtimeSourceKind.WebSocket:
                        source = new RealtimeBoardSnapshotSource(_webSocket, _jsonSettings, realProductCode);
                        _webSocketSources[source.Channel] = source;
                        break;
                }
                return source.Publish().RefCount();
            });
        }
    }
}
