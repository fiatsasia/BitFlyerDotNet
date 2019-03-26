//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Reactive.Disposables;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocket4Net;

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
        WebSocket _webSocket;
        JsonSerializerSettings _jsonSettings;

        public string Channel { get; private set; }
        IObserver<TSource> _observer;

        public RealtimeSourceBase(WebSocket webSocket, string channelFormat, JsonSerializerSettings jsonSettings, string productCode)
        {
            _webSocket = webSocket;
            _jsonSettings = jsonSettings;
            Channel = string.Format(channelFormat, productCode);
        }

        public void Subscribe()
        {
            if (_webSocket != null)
            {
                _webSocket.Send(JsonConvert.SerializeObject(new { method = "subscribe", @params = new { channel = Channel } }));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public IDisposable Subscribe(IObserver<TSource> observer)
        {
            _observer = observer;
            Subscribe();
            return Disposable.Create(OnDispose);
        }

        void OnDispose()
        {
            if (_webSocket != null)
            {
                _webSocket.Send(JsonConvert.SerializeObject(new { method = "unsubscribe", @params = new { channel = Channel }}));
            }
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
