//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Text;
using System.Diagnostics;
using System.Reactive.Disposables;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;

namespace BitFlyerDotNet.LightningApi
{
    internal abstract class RealtimePrivateSourceBase<TSource> : IRealtimeSource, IObservable<TSource> where TSource : class
    {
        WebSocketChannels _channels;
        JsonSerializerSettings _jsonSettings;

        public string Channel { get; private set; }
        string _authCommand;
        IObserver<TSource> _observer;

        public RealtimePrivateSourceBase(WebSocketChannels channels, string channel, JsonSerializerSettings jsonSettings, string key, string secret)
        {
            _channels = channels;
            _jsonSettings = jsonSettings;
            Channel = channel;

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var nonce = Guid.NewGuid().ToString("N");
            var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var sign = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{now}{nonce}"))).Replace("-", string.Empty).ToLower();
            _authCommand = JsonConvert.SerializeObject(new { method = "auth", @params = new { api_key = key, timestamp = now, nonce, signature = sign } });
        }

        public void Subscribe()
        {
            Debug.Print("Authentication will be send");
            Debug.Print(_authCommand);
            _channels.Send(_authCommand); // エラーハンドリングをどうするか？
            //_webSocket.Send(JsonConvert.SerializeObject(new { method = "subscribe", @params = new { channel = Channel } }));
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
