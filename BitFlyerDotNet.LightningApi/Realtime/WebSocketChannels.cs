//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
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
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace BitFlyerDotNet.LightningApi
{
    class WebSocketChannels : IDisposable
    {
        public static int WebSocketReconnectionIntervalMs { get; set; } = 3000;
        public long TotalReceivedMessageChars { get; private set; }
        public bool IsOpened => (_socket?.State ?? WebSocketState.None) == WebSocketState.Open;

        public event Action Opened;
        public event Action Suspended;
        public event Action Resumed;
        public Action<string> MessageSent;
        public Action<string, object> MessageReceived;

        ClientWebSocket _socket;
        WebSocketStream _istream;
        WebSocketStream _ostream;
        Thread _receiver;
        Task _receiveTask;

        Timer _reconnectionTimer;
        AutoResetEvent _openedEvent = new (false);
        ConcurrentDictionary<string, IRealtimeSource> _webSocketSources = new();
        CancellationToken _ct;
        string _uri;
        string _apiKey;
        HMACSHA256 _hash;
        bool _isWasm;

        public WebSocketChannels(string uri)
        {
            _isWasm = RuntimeInformation.OSArchitecture == /*Architecture.Wasm*/(Architecture)4; // implemented .NET5 or later
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

            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                _socket = new();

                //==================================================
                // Below option must be needed. If omit, serever will disconnect connection but not supported Blazer WebAssembly. 
                // Feb/2021
                if (!_isWasm) // implemented .NET5 or later
                {
                    _socket.Options.KeepAliveInterval = TimeSpan.Zero;
                }

                _istream = new WebSocketStream(_socket);
                _ostream = new WebSocketStream(_socket);

                _reconnectionTimer = new Timer(OnReconnection);

                //_socket.DataReceived += OnDataReceived;
                //_socket.Error += OnError;
            }
            catch (AggregateException)
            {
            }
        }

        public void Dispose()
        {
            Log.Trace($"{nameof(WebSocketChannels)} disposing...");
            _ct.ThrowIfCancellationRequested();
            if (_socket.State == WebSocketState.Open)
            {
                _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            //_socket.Dispose();
            Opened = null;
            MessageSent = null;
            MessageReceived = null;
            Log.Trace($"{nameof(WebSocketChannels)} disposed");
        }

        public async Task TryOpenAsync()
        {
            if (_socket.State != WebSocketState.Open)
            {
                Log.Trace("Opening WebSocket...");
                try
                {
                    await _socket.ConnectAsync(new Uri(_uri), CancellationToken.None);
                    if (_isWasm)
                    {
                        _receiveTask = Task.Run(ReaderThread); // WASM does not support Thread.Start()
                    }
                    else
                    {
                        _receiver = new Thread(ReaderThread) { IsBackground = true };
                        _receiver.Start();
                    }

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
        internal async void ReaderThread()
        {
            Log.Trace("Start reader thread loop");
            _ct = new();
            var json = string.Empty;
            while (true)
            {
                try
                {
                    var length = await _istream.ReadAsync(buffer, 0, buffer.Length, _ct);
                    if (length == 0)
                    {
                        Log.Info("WebSocket ReadAsync respond empty. Disconnected from client or probably disconnected from the server.");
                        OnClosed();
                        return; // Thread will be restarted.
                    }

                    json = Encoding.UTF8.GetString(buffer, 0, length);
                    OnMessageReceived(json);
                    WsReceived?.Invoke(json);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Log.Warn($"WebSocket ReaderThread caused exception: {ex.Message} '{json}'");
                }
            }
        }

        public void RegisterSource(IRealtimeSource source)
        {
            _webSocketSources[source.ChannelName] = source;
        }

        public void Send(string json)
        {
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            _ = _ostream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
            _ = _ostream.FlushAsync(CancellationToken.None);
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
            //Log.Trace($"Socket message received : {json}");
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
            if (_socket.State != WebSocketState.Open) // from Close() or Dispose()
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
                        _socket.ConnectAsync(new Uri(_uri), CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message);
                        _reconnectionTimer.Change(WebSocketReconnectionIntervalMs, Timeout.Infinite); // restart
                    }
                    break;

                case WebSocketState.Aborted:
                    _ct.ThrowIfCancellationRequested();
                    _socket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
                    _ = TryOpenAsync();
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
