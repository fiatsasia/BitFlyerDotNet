//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using WebSocket4Net;

namespace BitFlyerDotNet.LightningApi
{
    class WebSocketChannels : IDisposable
    {
        public static int WebSocketReconnectionIntervalMs { get; set; } = 5000;
        public long TotalReceivedMessageChars { get; private set; }

        public event Action Opened;
        public Action<string> MessageSent;
        public Action<string, object> MessageReceived;

        WebSocket _webSocket;
        Timer _reconnectionTimer;
        AutoResetEvent _openedEvent = new AutoResetEvent(false);
        AutoResetEvent _closedEvent = new AutoResetEvent(false);
        ConcurrentDictionary<string, IRealtimeSource> _webSocketSources = new ConcurrentDictionary<string, IRealtimeSource>();

        string _apiKey;
        HMACSHA256 _hash;

        public WebSocketChannels(string uri)
        {
            _webSocket = new WebSocket(uri);
            _webSocket.Security.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            _reconnectionTimer = new Timer(OnReconnection);

            _webSocket.Opened += OnOpened;
            _webSocket.Closed += OnClosed;
            _webSocket.MessageReceived += OnMessageReceived;
            _webSocket.DataReceived += OnDataReceived;
            _webSocket.Error += OnError;

            _openedEvent.Reset();
        }

        public WebSocketChannels(string uri, string apiKey, string apiSecret)
            : this(uri)
        {
            _apiKey = apiKey;
            _hash = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret));
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
        public void TryOpen()
        {
            if (!_opened)
            {
                _opened = true;

                _webSocket.OpenAsync();
                _openedEvent.WaitOne(10000);

                if (!string.IsNullOrEmpty(_apiKey) && _hash != null)
                {
                    Authenticate();
                }
            }
        }

        public void Close()
        {
            _opened = false;
            _webSocket.CloseAsync();
            _closedEvent.WaitOne(1000);
            _webSocket.Dispose();
            Opened = null;
            MessageSent = null;
            MessageReceived = null;
        }

        public void RegisterSource(IRealtimeSource source)
        {
            _webSocketSources[source.ChannelName] = source;
        }

        public void Send(string message)
        {
            _webSocket.Send(message);
            MessageSent?.Invoke(message);
        }

        bool Authenticate()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var nonce = Guid.NewGuid().ToString("N");
            var sign = BitConverter.ToString(_hash.ComputeHash(Encoding.UTF8.GetBytes($"{now}{nonce}"))).Replace("-", string.Empty).ToLower();
            var authCommand = JsonConvert.SerializeObject(new
            {
                method = "auth",
                @params = new
                {
                    api_key = _apiKey,
                    timestamp = now,
                    nonce,
                    signature = sign,
                },
                id = 1,
            });

            // Send auto command and wait response synchronously
            var jsonResult = "";
            var resultReceived = new AutoResetEvent(false);
            void OnAuthenticateResultReceived(object sender, MessageReceivedEventArgs args)
            {
                jsonResult = args.Message;
                resultReceived.Set();
            }
            _webSocket.MessageReceived += OnAuthenticateResultReceived;
            Send(authCommand);
            resultReceived.WaitOne();
            _webSocket.MessageReceived -= OnAuthenticateResultReceived;

            // Parse auth result
            var joResult = (JObject)JsonConvert.DeserializeObject(jsonResult);
            return joResult["result"].Value<bool>();
        }

        void OnOpened(object sender, EventArgs args)
        {
            Debug.WriteLine("{0} WebSocket opened.", DateTime.Now);
            _reconnectionTimer.Change(Timeout.Infinite, Timeout.Infinite); // stop
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
            //Debug.WriteLine($"Socket message received : {args.Message}");
            TotalReceivedMessageChars += args.Message.Length;
            var subscriptionResult = JObject.Parse(args.Message)["params"];
            if (subscriptionResult != null)
            {
                var channel = subscriptionResult["channel"].Value<string>();
                var message = _webSocketSources[channel].OnMessageReceived(subscriptionResult["message"]);
                MessageReceived?.Invoke(args.Message, message);
            }
            //else (on receive auth result message)
        }

        void OnDataReceived(object sender, WebSocket4Net.DataReceivedEventArgs args)
        {
            Debug.WriteLine($"Socket data received : Length={args.Data.Length}");
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
            if (!_opened)
            {
                return;
            }
            Debug.WriteLine("{0} WebSocket connection closed. Will be reopening...", DateTime.Now);
            _reconnectionTimer.Change(WebSocketReconnectionIntervalMs, Timeout.Infinite);
            _closedEvent.Set();
        }

        void OnReconnection(object _)
        {
            _reconnectionTimer.Change(Timeout.Infinite, Timeout.Infinite); // stop
            Debug.WriteLine("{0} WebSocket is reopening connection... state={1}", DateTime.Now, _webSocket.State);
            switch (_webSocket.State)
            {
                case WebSocketState.None:
                case WebSocketState.Closed:
                    try
                    {
                        _webSocket.Open(); // Does it need auth if authenticated ?
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        _reconnectionTimer.Change(WebSocketReconnectionIntervalMs, Timeout.Infinite); // restart
                    }
                    break;

                case WebSocketState.Open:
                    Debug.WriteLine("{0} Web socket is still opened.", DateTime.Now);
                    break;

                default:
                    _reconnectionTimer.Change(WebSocketReconnectionIntervalMs, Timeout.Infinite); // restart
                    break;
            }
        }
    }
}
