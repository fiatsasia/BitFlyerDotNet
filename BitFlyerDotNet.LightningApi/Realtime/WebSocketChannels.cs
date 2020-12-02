//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

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

        ClientWebSocket _socket;
        WebSocketStream _istream;
        WebSocketStream _ostream;
        Task _receiver;

        Timer _reconnectionTimer;
        AutoResetEvent _openedEvent = new (false);
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
            _socket?.Dispose();
            _opened = false;

            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                _socket = new();
                _socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);

                _istream = new WebSocketStream(_socket);
                _ostream = new WebSocketStream(_socket);

                _reconnectionTimer = new Timer(OnReconnection);

                //_socket.DataReceived += OnDataReceived;
                //_socket.Error += OnError;
            }
            catch (AggregateException ex)
            {

            }
        }

        public void Dispose()
        {
            Log.Trace($"{nameof(WebSocketChannels)}.Dispose");
            _opened = false;
            _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait();
            _socket.Dispose();
            Log.Trace($"{nameof(WebSocketChannels)}.Dispose exit");
        }

        bool _opened = false;
        public void TryOpen()
        {
            if (!_opened)
            {
                Log.Trace("Opening WebSocket...");
                try
                {
                    _opened = true;
                    _socket.ConnectAsync(new Uri(_uri), CancellationToken.None).Wait();
                    _receiver = Task.Run(ReaderThread);

                    if (!string.IsNullOrEmpty(_apiKey) && _hash != null)
                    {
                        Authenticate();
                    }

                    OnOpened();
                }
                catch (AggregateException ex)
                {
                    var wsEx = ex.InnerExceptions.FirstOrDefault() as WebSocketException;
                    if (wsEx != null)
                    {
                        Log.Warn($"WebSocket failed to connect to server. {wsEx.Message}");
                    }
                    throw;
                }
            }
        }

        // OrderBook message is the largest that around 20K bytes
        const int BufferSize = 1024*32; // 32K
        byte[] buffer = new byte[BufferSize];
        event Action<string> WsReceived;
        async void ReaderThread()
        {
            while (true)
            {
                var length = await _istream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);
                if (length == 0)
                {
                    Log.Info("WebSocket ReadAsync respond empty. Disconnected from client or probably disconnected from the server.");
                    OnClosed();
                    return; // Thread will be restarted.
                }
                try
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, length);
                    OnMessageReceived(json);
                    WsReceived?.Invoke(json);
                }
                catch (Exception ex)
                {
                    Log.Warn($"WebSocket ReaderThread caused exception: {ex.Message}");
                    // Call OnError
                    // Rethrow exception - after alpha
                }
            }
        }

        public void Close()
        {
            Log.Trace("Closing WebSocket...");
            _opened = false;
            _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait();
            _socket.Dispose();
            Opened = null;
            MessageSent = null;
            MessageReceived = null;
        }

        public void RegisterSource(IRealtimeSource source)
        {
            _webSocketSources[source.ChannelName] = source;
        }

        public void Send(string json)
        {
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            _ostream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
            _ostream.FlushAsync(CancellationToken.None).Wait();
            Log.Trace($"WebSocket sent: {json}");
            MessageSent?.Invoke(json);
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
            void OnAuthenticateResultReceived(string json)
            {
                Log.Trace($"WebSocket authention result received. '{json}'");
                jsonResult = json;
                resultReceived.Set();
            }

            WsReceived += OnAuthenticateResultReceived;
            Log.Trace("WebSocket sending authentication message..");
            Send(authCommand);
            Log.Trace("WebSocket sent authentication message.");
            resultReceived.WaitOne();
            WsReceived -= OnAuthenticateResultReceived;

            // Parse auth result
            var joResult = (JObject)JsonConvert.DeserializeObject(jsonResult);

            var authResult = joResult["result"].Value<bool>();
            Log.Info($"WebSocket authenticated. result = {authResult}");
            return authResult;
        }

        void OnOpened()
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
        }

        void OnMessageReceived(string json)
        {
            //Log.Trace($"Socket message received : {args.Message}");
            TotalReceivedMessageChars += json.Length;
            var subscriptionResult = JObject.Parse(json)["params"];
            if (subscriptionResult != null)
            {
                var channel = subscriptionResult["channel"].Value<string>();
                var message = _webSocketSources[channel].OnMessageReceived(subscriptionResult["message"]);
                MessageReceived?.Invoke(json, message);
            }
            //else (on receive auth result message)
        }

        public event Action<WebSocketErrorStatus> Error;

        void OnClosed()
        {
            if (!_opened) // from Close() or Dispose()
            {
                return;
            }

            Log.Error("WebSocket connection closed. Will be reopening...");
            Task.Run(() => { Suspended?.Invoke(); });
            _reconnectionTimer.Change(WebSocketReconnectionIntervalMs, Timeout.Infinite);
        }

        void OnReconnection(object _)
        {
            _reconnectionTimer.Change(Timeout.Infinite, Timeout.Infinite); // stop
            Log.Info($"WebSocket is reopening connection... state={_socket.State}");
            switch (_socket.State)
            {
                case WebSocketState.None:
                case WebSocketState.Closed:
                    try
                    {
                        _socket.ConnectAsync(new Uri(_uri), CancellationToken.None).Wait();
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
