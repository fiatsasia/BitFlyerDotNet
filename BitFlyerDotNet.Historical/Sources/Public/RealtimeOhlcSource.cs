//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    class RealtimeOhlcSource : IObservable<RealtimeOhlc>
    {
        IObservable<RealtimeOhlc> _source;
        CompositeDisposable _disposables = new CompositeDisposable();

        public delegate void RealtimeOhlcUpdated(RealtimeOhlc ohlc);

        public RealtimeOhlcSource(ExecutionCachedSourceFactory historicalFactory, BfProductCode productCode, TimeSpan frameSpan, IObservable<BfaExecution> realtime)
        {
            IDisposable subscribed = null;
            _source = Observable.Create<RealtimeOhlc>(observer =>
            {
                var firstOhlc = default(RealtimeOhlc);
                var currentOhlc = default(RealtimeOhlc);

                return realtime.Scan(default(BfaExecution), (prev, current) =>
                {
                    if (prev == default(BfaExecution))
                    {
                        firstOhlc = currentOhlc = new RealtimeOhlc(current);
                        var refTime = current.ExecutedTime.Round(frameSpan);
                        subscribed = historicalFactory.GetExecutionCachedSource(productCode, current.ExecutionId)
                        .TakeWhile(tick => tick.ExecutedTime >= refTime)
                        .Finally(() =>
                        {
                            subscribed.Dispose(); // 本当に必要か？
                        })
                        .Subscribe(
                            histTick => firstOhlc.Update(histTick),
                            ex => observer.OnError(ex),
                            () => { /* TakeWhile完了後にちゃんと終わるか？ */ }
                        );
                        observer.OnNext(firstOhlc);
                    }
                    // Close/Open buffer
                    else if (prev.ExecutedTime.Round(frameSpan) != current.ExecutedTime.Round(frameSpan))
                    {
                        currentOhlc.CommitFrame();
                        currentOhlc = new RealtimeOhlc(current);
                        observer.OnNext(currentOhlc);
                    }
                    else
                    {
                        currentOhlc.Update(current);
                    }
                    return current;
                }).Subscribe().AddTo(_disposables);
            });
        }

        public IDisposable Subscribe(IObserver<RealtimeOhlc> observer)
        {
            return _source.Subscribe(
                ohlc => observer.OnNext(ohlc),
                ex => observer.OnError(ex),
                () => observer.OnCompleted()
            ).AddTo(_disposables);
        }
    }
}
