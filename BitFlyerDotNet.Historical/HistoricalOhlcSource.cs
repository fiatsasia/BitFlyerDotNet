//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Fiats.Utils;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public class HistoricalOhlcSource : IObservable<IBfOhlc>
    {
        HistoricalDbContext _dbctx;

        IObservable<IBfOhlc> _source;
        CompositeDisposable _disposable = new CompositeDisposable();

        public HistoricalOhlcSource(BfProductCode productCode, TimeSpan frameSpan, DateTime endFrom, TimeSpan span, string cacheFolderBasePath)
        {
            // Create database file if not exists automatically
            _dbctx = new HistoricalDbContext(productCode, cacheFolderBasePath, string.Format("OHLC_{0}", Convert.ToInt32(frameSpan.TotalMinutes)));
            var requestedCount = Convert.ToInt32(span.TotalMinutes / frameSpan.TotalMinutes);
            endFrom = endFrom.Round(frameSpan);
            var startTo = endFrom - span + frameSpan;
            var end = endFrom - span + frameSpan;

            _source = Observable.Create<IBfOhlc>(observer =>
            {
                var query = _dbctx.Ohlcs.Where(e => e.Start <= endFrom && e.Start >= end).OrderByDescending(e => e.Start);
                if (query.Count() == requestedCount)
                {
                    query.ForEach(ohlc => observer.OnNext(ohlc));
                }
                else
                {
                    // Cryptowatch accepts close-time based range
                    CryptowatchOhlcSource.Get(productCode, frameSpan, endFrom + frameSpan, startTo + frameSpan).OrderByDescending(e => e.Start).ForEach(ohlc =>
                    {
                        if (!_dbctx.Ohlcs.Any(e => e.Start == ohlc.Start))
                        {
                            _dbctx.Ohlcs.Add(ohlc);
                        }
                        observer.OnNext(ohlc);
                    });
                    _dbctx.SaveChanges();
                }
                observer.OnCompleted();
                return () => 
                { };
            });

            // Cryptowatchの取得リミットに到達していた場合の対処
        }

        public IDisposable Subscribe(IObserver<IBfOhlc> observer)
        {
            return _source.Subscribe(observer).AddTo(_disposable);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
