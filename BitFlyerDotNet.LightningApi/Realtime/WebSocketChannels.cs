//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using WebSocket4Net;
using System.Threading.Tasks;

namespace BitFlyerDotNet.LightningApi
{
    class WebSocketChannels : IDisposable
    {
        public static int WebSocketReconnectionIntervalMs { get; set; } = 3000;
        public long TotalReceivedMessageChars { get; private set; }

        public event Action Opened;
        public event Action Suspended;
        public event Action Resumed;
        public Action<string> MessageSent;
        public Action<string, object> MessageReceived;

        WebSocket _webSocket;
        Timer _reconnectionTimer;
        AutoResetEvent _openedEvent = new (false);
        AutoResetEvent _closedEvent = new (false);
        ConcurrentDictionary<string, IRealtimeSource> _webSocketSources = new ();

        string _uri;
        string _apiKey;
        HMACSHA256 _hash;

        public WebSocketChannels(string uri)
        {
            _uri = uri;
            CreateWebSocket();
        }

        public WebSocketChannels(string uri, string apiKey, string apiSecret)
            : this(uri)
        {
            _apiKey = apiKey;
            _hash = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret));
        }

        void CreateWebSocket()
        {
            Log.Trace($"Creating WebSocket...");
            _webSocket?.Dispose();
            _opened = false;

            _webSocket = new WebSocket(_uri);
            _webSocket.Security.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            _reconnectionTimer = new Timer(OnReconnection);

            _webSocket.Opened += OnOpened;
            _webSocket.Closed += OnClosed;
            _webSocket.MessageReceived += OnMessageReceived;
            _webSocket.DataReceived += OnDataReceived;
            _webSocket.Error += OnError;

            _openedEvent.Reset();
        }

        public void Dispose()
        {
            Log.Trace($"{nameof(WebSocketChannels)}.Dispose");
            _opened = false;
            _webSocket.CloseAsync();
            _closedEvent.WaitOne(1000);
            _webSocket.Dispose();
            Log.Trace($"{nameof(WebSocketChannels)}.Dispose exit");
        }

        bool _opened = false;
        public void TryOpen()
        {
            if (!_opened)
            {
                Log.Trace("Opening WebSocket...");
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
            Log.Trace("Closing WebSocket...");
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
            Log.Trace("WebSocket start authentication.");
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
                Log.Trace($"WebSocket authention result received. '{args.Message}'");
                jsonResult = args.Message;
                resultReceived.Set();
            }

            _webSocket.MessageReceived += OnAuthenticateResultReceived;
            Log.Trace("WebSocket sending authentication message..");
            Send(authCommand);
            Log.Trace("WebSocket sent authentication message.");
            resultReceived.WaitOne();
            _webSocket.MessageReceived -= OnAuthenticateResultReceived;

            // Parse auth result
            var joResult = (JObject)JsonConvert.DeserializeObject(jsonResult);

            var authResult = joResult["result"].Value<bool>();
            Log.Info($"WebSocket authenticated. result = {authResult}");
            return authResult;
        }

        void OnOpened(object sender, EventArgs args)
        {
            Log.Trace("WebSocket opened.");
            _reconnectionTimer.Change(Timeout.Infinite, Timeout.Infinite); // stop
            Task.Run(() =>
            {
                if (_webSocketSources.Count > 0)
                {
                    if (!string.IsNullOrEmpty(_apiKey) && _hash != null)
                    {
                        Authenticate();
                    }

                    Resumed?.Invoke();

                    Log.Info("WebSocket recover subscriptions.");
                    _webSocketSources.Values.ForEach(source => { source.Subscribe(); }); // resubscribe
                }
                else
                {
                    Opened?.Invoke();
                }
            });
            _openedEvent.Set();
        }

        void OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            //Log.Trace($"Socket message received : {args.Message}");
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
            Log.Trace($"Socket data received : Length={args.Data.Length}");
        }

        public event Action<WebSocketErrorStatus> Error;
        void OnError(object sender, SuperSocket.ClientEngine.ErrorEventArgs args)
        {
            Log.Error($"Socket error : {args.Exception.Message}");
            // Classifies expected or unexpexted
            var error = new WebSocketErrorStatus();
            switch (args.Exception)
            {
                case IOException ioex:
                    error.Message = (ioex.InnerException != null) ? ioex.InnerException.Message : ioex.Message;
                    break;

                case SocketException sockex:
                    Log.Error($"Caused socket exception. Will be closed. code:{sockex.SocketErrorCode} {sockex.Message}");
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
            if (!_opened) // from Close() or Dispose()
            {
                _closedEvent.Set();
                return;
            }

            Log.Error("WebSocket connection closed. Will be reopening...");
            Task.Run(() => { Suspended?.Invoke(); });
            _reconnectionTimer.Change(WebSocketReconnectionIntervalMs, Timeout.Infinite);
        }

        void OnReconnection(object _)
        {
            _reconnectionTimer.Change(Timeout.Infinite, Timeout.Infinite); // stop
            Log.Info($"WebSocket is reopening connection... state={_webSocket.State}");
            switch (_webSocket.State)
            {
                case WebSocketState.None:
                case WebSocketState.Closed:
                    try
                    {
                        //CreateWebSocket();
                        //TryOpen();
                        _webSocket.OpenAsync(); // Open() だと返ってこない場合がある。
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message);
                        _reconnectionTimer.Change(WebSocketReconnectionIntervalMs, Timeout.Infinite); // restart
                    }
                    break;

                case WebSocketState.Open:
                    Log.Warn("Web socket is still opened.");
                    break;

                default:
                    _reconnectionTimer.Change(WebSocketReconnectionIntervalMs, Timeout.Infinite); // restart
                    break;
            }
        }
    }
}
