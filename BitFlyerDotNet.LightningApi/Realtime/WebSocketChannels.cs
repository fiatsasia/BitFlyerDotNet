//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using WebSocket4Net;

namespace BitFlyerDotNet.LightningApi
{
    internal class WebSocketChannels : IDisposable
    {
        const int WebSocketReconnectionIntervalMs = 5000;

        WebSocket _webSocket;
        Timer _wsReconnectionTimer;
        AutoResetEvent _openedEvent = new AutoResetEvent(false);
        AutoResetEvent _closedEvent = new AutoResetEvent(false);

        ConcurrentDictionary<string, IRealtimeSource> _webSocketSources = new ConcurrentDictionary<string, IRealtimeSource>();

        public WebSocketChannels(string uri)
        {
            _webSocket = new WebSocket(uri);
            _webSocket.Security.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            _wsReconnectionTimer = new Timer(OnReconnection);

            _webSocket.Opened += OnOpened;
            _webSocket.Closed += OnClosed;
            _webSocket.MessageReceived += OnMessageReceived;
            _webSocket.Error += OnError;

            _openedEvent.Reset();
        }

        public void Dispose()
        {
            Debug.Print($"{nameof(WebSocketChannels)}.Dispose");
            _webSocket.CloseAsync();
            _closedEvent.WaitOne(1000);
            _webSocket.Dispose();
            Debug.Print($"{nameof(WebSocketChannels)}.Dispose exit");
        }

        bool _opened = false;
        public void Open()
        {
            if (!_opened)
            {
                _opened = true;

                _webSocket.OpenAsync();
                _openedEvent.WaitOne(10000);
            }
        }

        public void RegisterSource(IRealtimeSource source)
        {
            _webSocketSources[source.Channel] = source;
        }

        public void Send(string message)
        {
            _webSocket?.Send(message);
        }

        public event Action Opened;
        void OnOpened(object sender, EventArgs args)
        {
            Debug.WriteLine("{0} WebSocket opened.", DateTime.Now);
            _wsReconnectionTimer.Change(Timeout.Infinite, Timeout.Infinite); // stop
            if (_webSocketSources.Count > 0)
            {
                Debug.WriteLine("{0} WebSocket recover subscriptions.", DateTime.Now);
                _webSocketSources.Values.ForEach(source => { source.Subscribe(); }); // resubscribe
            }
            Opened?.Invoke();
            _openedEvent.Set();
        }

        void OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            //Debug.WriteLine($"Socket received : {e.Message}");
            var subscriptionResult = JObject.Parse(args.Message)["params"];
            var channel = subscriptionResult["channel"].Value<string>();
            _webSocketSources[channel].OnSubscribe(subscriptionResult["message"]);
        }

        public event Action<WebSocketErrorStatus> Error;
        void OnError(object sender, SuperSocket.ClientEngine.ErrorEventArgs args)
        {
            Debug.WriteLine($"Socket error : {args.Exception.Message}");
            // Classifies expected or unexpexted
            var error = new WebSocketErrorStatus();
            switch (args.Exception)
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
                    switch ((uint)args.Exception.HResult)
                    {
                        case 0x80131500: // Bad gateway - probably terminated from host
                            error.Message = args.Exception.Message;
                            break;

                        default: // Unexpected exception
                            throw args.Exception;
                    }
                    break;
            }
            Error?.Invoke(error);
        }

        void OnClosed(object sender, EventArgs args)
        {
            Debug.WriteLine("{0} WebSocket connection closed. Will be reopening...", DateTime.Now);
            _wsReconnectionTimer.Change(WebSocketReconnectionIntervalMs, Timeout.Infinite);
            _closedEvent.Set();
        }

        void OnReconnection(object _)
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
        }
    }
}
