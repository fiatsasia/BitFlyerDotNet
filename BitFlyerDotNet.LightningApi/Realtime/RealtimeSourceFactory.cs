//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

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
        get { return WebSocketChannel.WebSocketReconnectionIntervalMs; }
        set { WebSocketChannel.WebSocketReconnectionIntervalMs = value; }
    }

    public long TotalReceivedMessageChars => Channel.TotalReceivedMessageChars;

    public event Action<WebSocketErrorStatus> Error
    {
        add { Channel.Error += value; }
        remove { Channel.Error -= value; }
    }

    public event Action ConnectionSuspended
    {
        add { Channel.Suspended += value; }
        remove { Channel.Suspended -= value; }
    }

    public event Action ConnectionResumed
    {
        add { Channel.Resumed += value; }
        remove { Channel.Resumed -= value; }
    }

    public static System.Reactive.Concurrency.IScheduler Scheduler { get; set; } = System.Reactive.Concurrency.Scheduler.Default;

    public WebSocketChannel Channel { get; private set; }

    CompositeDisposable _disposables = new ();
    public static RealtimeSourceFactory Singleton { get; } = new();

    /// <summary>
    /// Create public realtime source
    /// </summary>
    public RealtimeSourceFactory()
    {
        Channel = new WebSocketChannel(EndpointUrl).AddTo(_disposables);
    }

    /// <summary>
    /// Create pricate realtime source
    /// </summary>
    /// <param name="apiKey"></param>
    /// <param name="apiSecret"></param>
    public RealtimeSourceFactory(string apiKey, string apiSecret)
    {
        Channel = new WebSocketChannel(EndpointUrl, apiKey, apiSecret).AddTo(_disposables);
    }

    public bool Authenticate(string apiKey, string apiSecret)
    {
        return Channel.Authenticate(apiKey, apiSecret);
    }

    public void Dispose()
    {
        Log.Debug($"{nameof(RealtimeSourceFactory)} disposing...");
        _disposables.Dispose();
        Log.Debug($"{nameof(RealtimeSourceFactory)} diposed");
    }

    public bool IsOpened { get; private set; }
    public async Task<bool> TryOpenAsync()
    {
        if (IsOpened)
        {
            return false;
        }

        await Channel.TryOpenAsync();
        IsOpened = true;
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

    ConcurrentDictionary<string, IObservable<BfExecution>> _executionSources = new ();
    public IObservable<BfExecution> GetExecutionSource(string productCode, bool startPending = false)
    {
        var result = _executionSources.GetOrAdd(productCode, _ =>
        {
            var source = new RealtimeExecutionSource(Channel, productCode, s =>
            {
                _executionSources.TryRemove(s.ProductCode, out var _);
                OnSourceClosed();
            });
            Channel.RegisterSource(source);
            if (startPending)
            {
                return source.ObserveOn(Scheduler).SkipWhile(tick => tick.Id == 0).Publish();
            }
            else
            {
                return source.ObserveOn(Scheduler).SkipWhile(tick => tick.Id == 0).Publish().RefCount();
            }
        });

        return result;
    }

    public void StartExecutionSource(string productCode)
    {
        if (_executionSources.TryGetValue(productCode, out var source))
        {
            if (source is IConnectableObservable<BfExecution> publishSource)
            {
                publishSource.Connect().AddTo(_disposables);
            }
        }
    }

    public void StartAllExecutionSources()
    {
        foreach (var source in _executionSources.Values)
        {
            if (source is IConnectableObservable<BfExecution> publishSource)
            {
                publishSource.Connect().AddTo(_disposables);
            }
        }
    }

    ConcurrentDictionary<string, IObservable<BfTicker>> _tickSources = new ();
    public IObservable<BfTicker> GetTickerSource(string productCode)
    {
        return _tickSources.GetOrAdd(productCode, _ => // Cause ArgumentException if key not found.
        {
            var source = new RealtimeTickerSource(Channel, productCode, s =>
            {
                _tickSources.TryRemove(s.ProductCode, out var _);
                OnSourceClosed();
            });
            Channel.RegisterSource(source);
            return source.ObserveOn(Scheduler).Publish().RefCount();
        });
    }

    ConcurrentDictionary<string, IObservable<BfOrderBook>> _orderBookSnapshotSources = new ();
    public IObservable<BfOrderBook> GetOrderBookSource(string productCode)
    {
        return _orderBookSnapshotSources.GetOrAdd(productCode, _ => // Cause ArgumentException if key not found.
        {
            var snapshot = new RealtimeBoardSnapshotSource(Channel, productCode);
            var update = new RealtimeBoardSource(Channel, productCode);
            Channel.RegisterSource(snapshot);
            Channel.RegisterSource(update);
            var source = new BfOrderBookStream(productCode, snapshot, update, s =>
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

        var source = new RealtimeChildOrderEventsSource(Channel, s=>
        {    _childOrderEventSource = null;
            OnSourceClosed();
        });
        Channel.RegisterSource(source);
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

        var source = new RealtimeParentOrderEventsSource(Channel, s =>
        {
            _parentOrderEventSource = null;
            OnSourceClosed();
        });
        Channel.RegisterSource(source);
        _parentOrderEventSource = source.ObserveOn(Scheduler).Publish().RefCount();
        return _parentOrderEventSource;
    }
}
