//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Reactive.Disposables;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BitFlyerDotNet.LightningApi
{
    internal interface IRealtimeSource
    {
        string Channel { get; }
        void OnSubscribe(JToken token);
        void Subscribe();
    }

    internal abstract class RealtimeSourceBase<TSource> : IRealtimeSource, IObservable<TSource> where TSource : class
    {
        WebSocketChannels _channels;
        JsonSerializerSettings _jsonSettings;

        public string Channel { get; private set; }
        IObserver<TSource> _observer;

        public RealtimeSourceBase(WebSocketChannels channels, string channelFormat, JsonSerializerSettings jsonSettings, string productCode)
        {
            _channels = channels;
            _jsonSettings = jsonSettings;
            Channel = string.Format(channelFormat, productCode);
        }

        public void Subscribe()
        {
            _channels.Send(JsonConvert.SerializeObject(new { method = "subscribe", @params = new { channel = Channel } }));
        }

        public IDisposable Subscribe(IObserver<TSource> observer)
        {
            _observer = observer;
            Subscribe();
            return Disposable.Create(OnDispose);
        }

        void OnDispose()
        {
            _channels.Send(JsonConvert.SerializeObject(new { method = "unsubscribe", @params = new { channel = Channel }}));
            _observer?.OnCompleted();
            _observer = null;
        }

        public abstract void OnSubscribe(JToken token);

        protected void OnNext(string json)
        {
            _observer?.OnNext(JsonConvert.DeserializeObject<TSource>(json, _jsonSettings));
        }

        protected void OnNext(JToken token)
        {
            _observer?.OnNext(token.ToObject<TSource>());
        }

        protected void OnNextArray(JToken token)
        {
            var ticks = token.ToObject<TSource[]>();
            foreach (var tick in ticks)
            {
                _observer?.OnNext(tick);
            }
        }
    }
}
