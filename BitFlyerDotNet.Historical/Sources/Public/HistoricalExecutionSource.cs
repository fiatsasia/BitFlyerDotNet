//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    public class HistoricalExecutionSource : IObservable<IBfExecution>
    {
        const int ReadCountMax = 500;

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
                            continue;
                        }

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
