//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Reactive.Disposables;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BitFlyerDotNet.LightningApi
{
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
            //Log.Trace($"RealtimeSourceBase.{nameof(DispatchMessage)} observer is {_observer != null}");
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
