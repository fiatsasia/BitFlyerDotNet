//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Financial.Extensions;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public class HistoricalExecutionSource : IObservable<IBfExecution>
    {
        const int ReadCountMax = 500;
        static readonly TimeSpan ErrorInterval = TimeSpan.FromMilliseconds(15000); // Public API is limited 500 requests in a minute.
        static readonly TimeSpan ReadInterval = TimeSpan.FromMilliseconds(0);

        CancellationTokenSource _cancel = new CancellationTokenSource();
        CompositeDisposable _disposables = new CompositeDisposable();
        IObservable<IBfExecution> _source;

        public HistoricalExecutionSource(BitFlyerClient client, BfProductCode productCode, int before, int after, int readCount=ReadCountMax)
        {
            if (after <= 0)
            {
                throw new ArgumentException("after must not be 0.");
            }
            readCount = Math.Min(readCount, ReadCountMax);
            _source = Observable.Create<IBfExecution>(observer => {
                return Task.Run(() =>
                {
                    while (true)
                    {
                        var result = client.GetExecutions(productCode, ReadCountMax, before, 0);
                        if (result.IsError)
                        {
                            Thread.Sleep(ErrorInterval);
                            continue;
                        }
                        Thread.Sleep(ReadInterval);

                        var elements = result.GetResult();
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
