//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocket4Net;
using Fiats.Utils;

namespace BitFlyerDotNet.LightningApi
{
    public class RealtimeSourceFactory : IDisposable
    {
        public class ErrorStatus
        {
            public SocketError SocketError { get; set; } = SocketError.Success;
            public string Message { get; set; }
        }
        public delegate void RealtimeErrorHandler(ErrorStatus error);
        public RealtimeErrorHandler ErrorHandlers;

        CompositeDisposable _disposables = new CompositeDisposable();
        WebSocket _webSocket;
        AutoResetEvent _openedEvent = new AutoResetEvent(false);
        JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        ConcurrentDictionary<string, IRealtimeSource> _webSocketSources = new ConcurrentDictionary<string, IRealtimeSource>();
        Timer _wsReconnectionTimer;
        const int WebSocketReconnectionIntervalMs = 5000;

        BitFlyerClient _client = new BitFlyerClient();
        Dictionary<string, string> _productCodeAliases = new Dictionary<string, string>();

        public RealtimeSourceFactory()
        {
            _webSocket = new WebSocket("wss://ws.lightstream.bitflyer.com/json-rpc");
            _webSocket.Security.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            _wsReconnectionTimer = new Timer((_) =>
            {
                _wsReconnectionTimer.Change(Timeout.Infinite, Timeout.Infinite); // stop
                Debug.WriteLine("{0} WebSocket is reopening connection... state={1}", DateTime.Now, _webSocket.State);
                switch (_webSocket.State)
                {
                    case WebSocketState.None:
                    case WebSocketState.Closed:
                        try
                        {
                            _webSocket.Open();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            _wsReconnectionTimer.Change(WebSocketReconnectionIntervalMs, Timeout.Infinite); // restart
                        }
                        break;

                    case WebSocketState.Open:
                        Debug.WriteLine("{0} Web socket is still opened.", DateTime.Now);
                        break;

                    default:
                        _wsReconnectionTimer.Change(WebSocketReconnectionIntervalMs, Timeout.Infinite); // restart
                        break;
                }
            });
                            
            _openedEvent.Reset();
            _webSocket.Opened += (_, __) =>
            {
                Debug.WriteLine("{0} WebSocket opened.", DateTime.Now);
                _wsReconnectionTimer.Change(Timeout.Infinite, Timeout.Infinite); // stop
                if (_webSocketSources.Count > 0)
                {
                    Debug.WriteLine("{0} WebSocket recover subscriptions.", DateTime.Now);
                    _webSocketSources.Values.ForEach(source => { source.Subscribe(); }); // resubscribe
                }
                _openedEvent.Set();
            };

            _webSocket.MessageReceived += (_, e) =>
            {
                var subscriptionResult = JObject.Parse(e.Message)["params"];
                var channel = subscriptionResult["channel"].Value<string>();
                _webSocketSources[channel].OnSubscribe(subscriptionResult["message"]);
            };

            _webSocket.Error += (_, e) =>
            {
                // Classifies expected or unexpexted
                var error = new ErrorStatus();
                switch (e.Exception)
                {
                    case IOException ioex:
                        error.Message = (ioex.InnerException != null) ? ioex.InnerException.Message : ioex.Message;
                        break;

                    case SocketException sockex:
                        Debug.WriteLine("{0} WebSocket socket error({1})", DateTime.Now, sockex.SocketErrorCode);
                        Debug.WriteLine("{0} WebSocket caused exception. Will be closed.", DateTime.Now, sockex.Message);
                        error.SocketError = sockex.SocketErrorCode;
                        error.Message = sockex.Message;
                        break;

                    default:
                        switch ((uint)e.Exception.HResult)
                        {
                            case 0x80131500: // Bad gateway - probably terminated from host
                                error.Message = e.Exception.Message;
                                break;

                            default: // Unexpected exception
                                throw e.Exception;
                        }
                        break;
                }
                ErrorHandlers?.Invoke(error);
            };

            _webSocket.Closed += (_, __) =>
            {
                Debug.WriteLine("{0} WebSocket connection closed. Will be reopening...", DateTime.Now);
                _wsReconnectionTimer.Change(WebSocketReconnectionIntervalMs, Timeout.Infinite);
            };
        }

        bool _opened = false;
        void TryOpen()
        {
            if (!_opened)
            {
                _opened = true;
                _webSocket.Open();
                _openedEvent.WaitOne(10000);
            }
        }

        void InitProductCodeAliases()
        {
            if (_productCodeAliases.Count > 0)
            {
                return; // Already initialized
            }

            // Convert from future product code alias to real product code
            var resps = _client.GetMarketsAll();
            foreach (var resp in resps)
            {
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
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        ConcurrentDictionary<BfProductCode, IConnectableObservable<BfExecution>> _executionColdSources = new ConcurrentDictionary<BfProductCode, IConnectableObservable<BfExecution>>();
        public IObservable<BfExecution> GetExecutionSource(BfProductCode productCode, bool coldStart = false)
        {
            TryOpen();
            var result = _executionColdSources.GetOrAdd(productCode, _ =>
            {
                InitProductCodeAliases();
                var realProductCode = _productCodeAliases[productCode.ToEnumString()];
                var source = new RealtimeExecutionSource(_webSocket, _jsonSettings, realProductCode);
                _webSocketSources[source.Channel] = source;
                return source.SkipWhile(tick => tick.ExecutionId == 0).Publish();
            });

            return coldStart ? result : result.RefCount();
        }

        public void StartExecutionSource(BfProductCode productCode)
        {
            if (_executionColdSources.TryGetValue(productCode, out IConnectableObservable<BfExecution> source))
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

        ConcurrentDictionary<BfProductCode, IObservable<BfTicker>> _tickSources = new ConcurrentDictionary<BfProductCode, IObservable<BfTicker>>();
        public IObservable<BfTicker> GetTickerSource(BfProductCode productCode)
        {
            TryOpen();
            return _tickSources.GetOrAdd(productCode, _ =>
            {
                InitProductCodeAliases();
                var realProductCode = _productCodeAliases[productCode.ToEnumString()];
                var source = new RealtimeTickerSource(_webSocket, _jsonSettings, realProductCode);
                _webSocketSources[source.Channel] = source;
                return source.Publish().RefCount();
            });
        }

        ConcurrentDictionary<BfProductCode, IObservable<BfBoard>> _boardSources = new ConcurrentDictionary<BfProductCode, IObservable<BfBoard>>();
        public IObservable<BfBoard> GetBoardSource(BfProductCode productCode)
        {
            TryOpen();
            return _boardSources.GetOrAdd(productCode, _ =>
            {
                InitProductCodeAliases();
                var realProductCode = _productCodeAliases[productCode.ToEnumString()];
                var source = new RealtimeBoardSource(_webSocket, _jsonSettings, realProductCode);
                _webSocketSources[source.Channel] = source;
                return source.Publish().RefCount();
            });
        }

        ConcurrentDictionary<BfProductCode, IObservable<BfBoard>> _boardSnapshotSources = new ConcurrentDictionary<BfProductCode, IObservable<BfBoard>>();
        public IObservable<BfBoard> GetBoardSnapshotSource(BfProductCode productCode)
        {
            TryOpen();
            return _boardSnapshotSources.GetOrAdd(productCode, _ =>
            {
                InitProductCodeAliases();
                var realProductCode = _productCodeAliases[productCode.ToEnumString()];
                var source = new RealtimeBoardSnapshotSource(_webSocket, _jsonSettings, realProductCode);
                _webSocketSources[source.Channel] = source;
                return source.Publish().RefCount();
            });
        }
    }
}
