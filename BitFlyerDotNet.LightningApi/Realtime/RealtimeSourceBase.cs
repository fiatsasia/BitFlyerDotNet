//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Reactive.Disposables;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BitFlyerDotNet.LightningApi
{
    interface IRealtimeSource
    {
        string ChannelName { get; }
        object OnMessageReceived(JToken token);
        void Subscribe();
    }

    abstract class RealtimeSourceBase<TSource> : IRealtimeSource, IObservable<TSource> where TSource : class
    {
        WebSocketChannels _channels;

        public string ChannelName { get; private set; }
        IObserver<TSource> _observer;

        public RealtimeSourceBase(WebSocketChannels channels, string channelName)
        {
            _channels = channels;
            ChannelName = channelName;
        }

        public void Subscribe()
        {
            _channels.Send(JsonConvert.SerializeObject(new { method = "subscribe", @params = new { channel = ChannelName } }));
        }

        public IDisposable Subscribe(IObserver<TSource> observer)
        {
            _observer = observer;
            Subscribe();
            return Disposable.Create(OnDispose);
        }

        protected virtual void OnDispose()
        {
            _channels.Send(JsonConvert.SerializeObject(new { method = "unsubscribe", @params = new { channel = ChannelName }}));
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
}
