//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Financier;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public class HistoricalExecutionSource : IObservable<IBfExecution>
    {
        const int ReadCountMax = 500;
        static readonly TimeSpan ErrorInterval = TimeSpan.FromMilliseconds(15000); // Public API is limited 500 requests in 5 minutes.
        static readonly TimeSpan ReadInterval = TimeSpan.FromMilliseconds(600); // 600ms x 500times / 60s = 5mintes

        CancellationTokenSource _cancel = new CancellationTokenSource();
        CompositeDisposable _disposables = new CompositeDisposable();
        IObservable<IBfExecution> _source;

        public HistoricalExecutionSource(BitFlyerClient client, BfProductCode productCode, int before, int after, int readCount=ReadCountMax)
        {
            readCount = Math.Min(readCount, ReadCountMax);
            _source = Observable.Create<IBfExecution>(observer => {
                return Task.Run(() =>
                {
                    while (true)
                    {
                        var resp = client.GetExecutions(productCode, ReadCountMax, before, 0);
                        if (resp.IsError)
                        {
                            if (resp.StatusCode == HttpStatusCode.BadRequest) // no more records
                            {
                                observer.OnCompleted();
                                return;
                            }
                            Thread.Sleep(ErrorInterval);
                            continue;
                        }
                        Thread.Sleep(ReadInterval);

                        var elements = resp.GetContent();
                        foreach (var element in elements)
                        {
                            if (_cancel.IsCancellationRequested)
                            {
                                observer.OnCompleted();
                                return;
                            }
                            if (element.ExecutionId <= after)
                            {
                                observer.OnCompleted();
                                return;
                            }
                            observer.OnNext(element);
                        }
                        before = elements.Last().ExecutionId;
                    }
                });
            });
        }

        public IDisposable Subscribe(IObserver<IBfExecution> observer)
        {
            _source.Subscribe(observer).AddTo(_disposables);
            return Disposable.Create(() => _cancel.Cancel());
        }
    }
}
