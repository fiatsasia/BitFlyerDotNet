//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class WebSocketChannel : IDisposable
{
    public static int WebSocketReconnectionIntervalMs { get; set; } = 3000;
    public long TotalReceivedMessageChars { get; private set; }
    public bool IsOpened => (_socket?.State ?? WebSocketState.None) == WebSocketState.Open;
    public bool IsPrivate => _apiKey != default;

    public event Action Opened;
    public event Action Suspended;
    public event Action Resumed;
    public event Action<string> MessageSent;
    public event Action<object> MessageReceived;

    ClientWebSocket _socket = new();
    WebSocketStream _istream;
    WebSocketStream _ostream;
    Task _receiveTask;

    Timer _reconnectionTimer;
    AutoResetEvent _openedEvent = new (false);
    ConcurrentDictionary<string, IRealtimeSource> _webSocketSources = new();
    CancellationTokenSource _cts = new();
    string _uri;
    string _apiKey;
    string _apiSecret;

    public WebSocketChannel(string uri)
    {
        _uri = uri;
        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

        //==================================================
        // Below option must be needed. If omit, serever will disconnect connection but not supported Blazer WebAssembly. 
        // Feb/2022
        if (RuntimeInformation.OSArchitecture != /*Architecture.Wasm*/(Architecture)4) // implemented .NET5 or later
        {
            _socket.Options.KeepAliveInterval = TimeSpan.Zero;
        }

        _istream = new WebSocketStream(_socket);
        _ostream = new WebSocketStream(_socket);

        _reconnectionTimer = new Timer(async e => { await OnReconnection(); });

        //_socket.DataReceived += OnDataReceived;
        //_socket.Error += OnError;
    }

    public WebSocketChannel(string uri, string apiKey, string apiSecret)
        : this(uri)
    {
        _apiKey = apiKey;
        _apiSecret = apiSecret;
    }

    public void Dispose()
    {
        Log.Debug($"{nameof(WebSocketChannel)} disposing...");
        _cts.Cancel();
        if (_receiveTask != null)
        {
            Task.WaitAll(_receiveTask);
        }
        if (_socket.State == WebSocketState.Open)
        {
            _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }

        _socket.Dispose();
        Opened = null;
        MessageSent = null;
        MessageReceived = null;
        Log.Debug($"{nameof(WebSocketChannel)} disposed");
    }

    public async Task<bool> TryOpenAsync()
    {
        if (_socket.State == WebSocketState.Open)
        {
            return false;
        }

        try
        {
            Log.Debug("Opening WebSocket...");
            await _socket.ConnectAsync(new Uri(_uri), CancellationToken.None);
            _receiveTask = Task.Run(ReaderThread); // WASM does not support Thread.Start()

            if (IsPrivate)
            {
                Authenticate(_apiKey, _apiSecret);
            }

            OnOpened();
        }
        catch (AggregateException ex)
        {
            var wsEx = ex.InnerExceptions.FirstOrDefault() as WebSocketException;
            if (wsEx != null)
            {
                Log.Error("WebSocket failed to connect to server.", wsEx);
            }
            throw;
        }

        return true;
    }

    // OrderBook message is the largest that around 20K bytes
    const int BufferSize = 1024*32; // 32K
    byte[] buffer = new byte[BufferSize];
    event Action<string> WsReceived;
    internal async void ReaderThread()
    {
        Log.Debug("Start reader thread loop");
        string json;
        while (true)
        {
            try
            {
                var length = await _istream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                if (length == 0)
                {
                    Log.Warn("WebSocket ReadAsync respond empty. Disconnected from client or probably disconnected from the server.");
                    OnClosed();
                    return; // Thread will be restarted.
                }

                if (_cts.IsCancellationRequested)
                {
                    return;
                }

                json = Encoding.UTF8.GetString(buffer, 0, length);
                OnMessageReceived(json);
                WsReceived?.Invoke(json);
            }
            catch (TaskCanceledException)
            {
                return;
            }
            catch (WebSocketException ex)
            {
                var ie = ex.InnerException?.InnerException;
                if (ie != null && ie is SocketException se && se.ErrorCode == 995)
                {
                    Log.Debug("Reader thread completed");
                    return;
                }
                throw;
            }
            catch (Exception ex)
            {
                Log.Error("WebSocket ReaderThread caused exception", ex);
                throw;
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
        MessageSent?.Invoke(json);
    }

    public bool Authenticate(string apiKey, string apiSecret)
    {
        Log.Debug("WebSocket start authentication.");
        _apiKey = apiKey;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = Guid.NewGuid().ToString("N");
        var hash = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret));
        var sign = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes($"{now}{nonce}"))).Replace("-", string.Empty).ToLower();
        var authCommand = JsonConvert.SerializeObject(new
        {
            method = "auth",
            @params = new
            {
                api_key = apiKey,
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
            Log.Trace($"WebSocket authentication result received. '{json}'");
            jsonResult = json;
            resultReceived.Set();
        }

        WsReceived += OnAuthenticateResultReceived;
        Log.Debug("WebSocket sending authentication message..");
        Send(authCommand);
        Log.Debug("WebSocket sent authentication message.");
        resultReceived.WaitOne();
        WsReceived -= OnAuthenticateResultReceived;

        // Parse auth result
        var joResult = (JObject)JsonConvert.DeserializeObject(jsonResult);
        var errorResult = joResult["error"];
        if (errorResult != null)
        {
            Log.Error($"WebSocket authenticate failed. result = {errorResult["message"].Value<string>()}");
            return (errorResult["code"].Value<int>() == -32009);
        }

        var authResult = joResult["result"].Value<bool>();
        Log.Debug($"WebSocket authenticated. result = {authResult}");
        return authResult;
    }

    void OnOpened()
    {
        Log.Debug("WebSocket opened.");
        _reconnectionTimer.Change(Timeout.Infinite, Timeout.Infinite); // stop
        Task.Run(() =>
        {
            if (_webSocketSources.Count > 0)
            {
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
        TotalReceivedMessageChars += json.Length;
        var subscriptionResult = JObject.Parse(json)["params"];
        if (subscriptionResult != null)
        {
            var channel = subscriptionResult["channel"].Value<string>();
            var message = _webSocketSources[channel].OnMessageReceived(subscriptionResult["message"]);
            MessageReceived?.Invoke(message);
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

        Log.Info("WebSocket connection closed. Will be reopening...");
        Task.Run(() => { Suspended?.Invoke(); });
        _reconnectionTimer.Change(WebSocketReconnectionIntervalMs, Timeout.Infinite);
    }

    async Task OnReconnection()
    {
        _reconnectionTimer.Change(Timeout.Infinite, Timeout.Infinite); // stop
        Log.Info($"WebSocket is reopening connection... state={_socket.State}");
        switch (_socket.State)
        {
            case WebSocketState.None:
            case WebSocketState.Closed:
                try
                {
                    await _socket.ConnectAsync(new Uri(_uri), CancellationToken.None);
                    if (IsPrivate)
                    {
                        Authenticate(_apiKey, _apiSecret);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Reconnect WebSocket failed", ex);
                    _reconnectionTimer.Change(WebSocketReconnectionIntervalMs, Timeout.Infinite); // restart
                }
                break;

            case WebSocketState.Aborted:
                _cts.Cancel();
                await _socket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
                await TryOpenAsync();
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
