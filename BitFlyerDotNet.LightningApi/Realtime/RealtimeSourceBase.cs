//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

abstract class RealtimeSourceBase<TSource> : IRealtimeSource, IObservable<TSource> where TSource : class
{
    WebSocketChannel _channels;

    public string ChannelName { get; private set; }
    IObserver<TSource> _observer;

    public RealtimeSourceBase(WebSocketChannel channels, string channelName)
    {
        _channels = channels;
        ChannelName = channelName;
    }

    public void Subscribe()
    {
        var json = JsonConvert.SerializeObject(new { method = "subscribe", @params = new { channel = ChannelName } });
        Log.Debug("Sending subscribe message...");
        _channels.Send(json);
        Log.Debug($"Sent subscribe message: {json}");
    }

    public IDisposable Subscribe(IObserver<TSource> observer)
    {
        _observer = observer;
        Subscribe();
        return Disposable.Create(OnDispose);
    }

    protected virtual void OnDispose()
    {
        if (_channels.IsOpened)
        {
            var json = JsonConvert.SerializeObject(new { method = "unsubscribe", @params = new { channel = ChannelName } });
            Log.Debug("Sending unsubscribe message...");
            _channels.Send(json);
            Log.Debug($"Sent unsubscribe message: {json}");
        }
        _observer?.OnCompleted();
        _observer = null;
    }

    public abstract object OnMessageReceived(JToken token);

    protected object DispatchMessage(JToken token)
    {
        var message = token.ToObject<TSource>();
        _observer?.OnNext(message);
        return message;
    }

    protected object DispatchArrayMessage(JToken token)
    {
        var messages = token.ToObject<TSource[]>();
        foreach (var message in messages)
        {
            _observer?.OnNext(message);
        }
        return messages;
    }
}
