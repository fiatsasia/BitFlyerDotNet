//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public class HistoricalOhlcSource : IObservable<IOhlcvv>
    {
        IOhlcCache _cache;
        IObservable<IOhlcvv> _source;
        CompositeDisposable _disposable = new CompositeDisposable();

        public HistoricalOhlcSource(ICacheFactory cacheFactory, BfProductCode productCode, TimeSpan frameSpan, DateTime endFrom, TimeSpan span, string cacheFolderBasePath)
        {
            // Create database file if not exists automatically
            _cache = cacheFactory.GetOhlcCache(productCode, frameSpan);
            var requestedCount = Convert.ToInt32(span.TotalMinutes / frameSpan.TotalMinutes);
            endFrom = endFrom.Round(frameSpan);
            var startTo = endFrom - span + frameSpan;
            var end = endFrom - span + frameSpan;

            _source = Observable.Create<IOhlcvv>(observer =>
            {
                var query = _cache.GetOhlcsBackward(endFrom, span);
                if (query.Count() == requestedCount)
                {
                    query.ForEach(ohlc => observer.OnNext(ohlc));
                }
                else
                {
                    // Cryptowatch accepts close-time based range
                    CryptowatchOhlcSource.Get(productCode, frameSpan, endFrom + frameSpan, startTo + frameSpan).OrderByDescending(e => e.Start).ForEach(ohlc =>
                    {
                        _cache.Add(new DbHistoricalOhlc(ohlc, frameSpan));
                        observer.OnNext(ohlc);
                    });
                    _cache.SaveChanges();
                }
                observer.OnCompleted();
                return () => { };
            });

            // Cryptowatchの取得リミットに到達していた場合の対処
        }

        public IDisposable Subscribe(IObserver<IOhlcvv> observer)
        {
            return _source.Subscribe(observer).AddTo(_disposable);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
