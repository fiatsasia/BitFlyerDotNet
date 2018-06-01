//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using System.Reactive.Disposables;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PubNubMessaging.Core;
using Quobject.SocketIoClientDotNet.Client;
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
        Pubnub _pubnub;
        Socket _socket;
        WebSocket _webSocket;
        JsonSerializerSettings _jsonSettings;

        public string Channel { get; private set; }
        IObserver<TSource> _observer;

        public RealtimeSourceBase(Pubnub pubnub, string channelFormat, JsonSerializerSettings jsonSettings, string productCode)
        {
            _pubnub = pubnub;
            _jsonSettings = jsonSettings;
            Channel = string.Format(channelFormat, productCode);
        }

        public RealtimeSourceBase(Socket socket, string channelFormat, JsonSerializerSettings jsonSettings, string productCode)
        {
            _socket = socket;
            _jsonSettings = jsonSettings;
            Channel = string.Format(channelFormat, productCode);
            _socket.On(Channel, OnScoketSubscribe);
        }

        public RealtimeSourceBase(WebSocket webSocket, string channelFormat, JsonSerializerSettings jsonSettings, string productCode)
        {
            _webSocket = webSocket;
            _jsonSettings = jsonSettings;
            Channel = string.Format(channelFormat, productCode);
        }

        public void Subscribe()
        {
            if (_pubnub != null)
            {
                _pubnub.Subscribe<string>(Channel, OnPubnubSubscribe, OnPubnubConnect, OnPubnubError);
            }
            else if (_socket != null)
            {
                _socket.Emit("subscribe", Channel);
            }
            else if (_webSocket != null)
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
            if (_pubnub != null)
            {
                _pubnub.Unsubscribe<string>(Channel, OnPubnubSubscribe, OnPubnubConnect, OnPubnubDisconnect, OnPubnubError);
            }
            else if (_socket != null)
            {
                _socket.Emit("unsubscribe", Channel);
            }
            else if (_webSocket != null)
            {
                _webSocket.Send(JsonConvert.SerializeObject(new { method = "unsubscribe", @params = new { channel = Channel }}));
            }
            _observer?.OnCompleted();
            _observer = null;
        }

        void OnPubnubConnect(string json) { }
        void OnPubnubDisconnect(string json) { }
        void OnPubnubError(PubnubClientError error) { }

        protected abstract void OnPubnubSubscribe(string json);
        protected abstract void OnScoketSubscribe(object message);
        public abstract void OnWebSocketSubscribe(JToken token);

        protected void OnNext(string json)
        {
            _observer?.OnNext(JsonConvert.DeserializeObject<TSource>(json, _jsonSettings));
        }

        protected void OnNext(JToken token)
        {
            _observer?.OnNext(token.ToObject<TSource>());
        }

        protected void OnNextArray(string json)
        {
            var ticks = JsonConvert.DeserializeObject<TSource[]>(json, _jsonSettings);
            foreach (var tick in ticks)
            {
                _observer?.OnNext(tick);
            }
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
