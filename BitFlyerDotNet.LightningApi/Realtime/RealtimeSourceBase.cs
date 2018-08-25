//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;

using System.Reactive.Disposables;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if PUBNUB && DOTNETFRAMEWORK
using PubNubMessaging.Core;
#endif
using WebSocket4Net;

namespace BitFlyerDotNet.LightningApi
{
    internal interface IRealtimeSource
    {
        string Channel { get; }
        void OnWebSocketSubscribe(JToken token);
        void Subscribe();
    }

    internal abstract class RealtimeSourceBase<TSource> : IRealtimeSource, IObservable<TSource> where TSource : class
    {
#if PUBNUB && DOTNETFRAMEWORK
        Pubnub _pubnub;
#endif
        WebSocket _webSocket;
        JsonSerializerSettings _jsonSettings;

        public string Channel { get; private set; }
        IObserver<TSource> _observer;

#if PUBNUB && DOTNETFRAMEWORK
        public RealtimeSourceBase(Pubnub pubnub, string channelFormat, JsonSerializerSettings jsonSettings, string productCode)
        {
            _pubnub = pubnub;
            _jsonSettings = jsonSettings;
            Channel = string.Format(channelFormat, productCode);
        }
#endif

        public RealtimeSourceBase(WebSocket webSocket, string channelFormat, JsonSerializerSettings jsonSettings, string productCode)
        {
            _webSocket = webSocket;
            _jsonSettings = jsonSettings;
            Channel = string.Format(channelFormat, productCode);
        }

        public void Subscribe()
        {
#if PUBNUB && DOTNETFRAMEWORK
            if (_pubnub != null)
            {
                _pubnub.Subscribe<string>(). .Subscribe<string>(Channel, OnPubnubSubscribe, OnPubnubConnect, OnPubnubError);
            }
#endif
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
#if PUBNUB && DOTNETFRAMEWORK
            if (_pubnub != null)
            {
                _pubnub.Unsubscribe<string>(Channel, OnPubnubSubscribe, OnPubnubConnect, OnPubnubDisconnect, OnPubnubError);
            }
#endif
            if (_webSocket != null)
            {
                _webSocket.Send(JsonConvert.SerializeObject(new { method = "unsubscribe", @params = new { channel = Channel }}));
            }
            _observer?.OnCompleted();
            _observer = null;
        }

#if PUBNUB && DOTNETFRAMEWORK
        void OnPubnubConnect(string json) { }
        void OnPubnubDisconnect(string json) { }
        void OnPubnubError(PubnubClientError error) { }
#endif

        protected abstract void OnPubnubSubscribe(string json);
        public abstract void OnWebSocketSubscribe(JToken token);

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
