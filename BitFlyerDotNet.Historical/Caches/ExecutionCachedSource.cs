//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Financial.Extensions;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Historical
{
    class ExecutionCachedSource : IObservable<IBfExecution>
    {
        CompositeDisposable _disposable = new CompositeDisposable();
        IExecutionCache _cache;
        IObservable<IBfExecution> _source;
        CancellationTokenSource _cancel = new CancellationTokenSource();

        BitFlyerClient _client;
        BfProductCode _productCode;

        public ExecutionCachedSource(BitFlyerClient client, IExecutionCache cache, BfProductCode productCode, int before)
        {
            _client = client;
            _productCode = productCode;
            _cache = cache;

            Debug.WriteLine($"{nameof(ExecutionCachedSource)} constructed Before={before}");

            var manageRecords = _cache.GetManageTable();
            _source = (manageRecords.Count == 0) ? CreateSimpleCopySource(before, 0) : CreateMergedSource(manageRecords, before);
        }

        const int ReadCount = 500;
        int ReadInterval = 3000; // Public API is limited 500 requests in a minute.
        IObservable<IBfExecution> GetExecutions(int before, int after)
        {
            return Observable.Create<IBfExecution>(observer => { return Task.Run(() =>
            {
                while (true)
                {
                    var result = _client.GetExecutions(_productCode, ReadCount, before, after);
                    if (result.IsError)
                    {
                        Thread.Sleep(ReadInterval);
                        continue;
                    }
                    Thread.Sleep(100);

                    var elements = result.GetResult();
                    foreach (var element in elements)
                    {
                        if (_cancel.IsCancellationRequested)
                        {
                            observer.OnCompleted();
                            return;
                        }
                        observer.OnNext(element);
                    }
                    if (elements.Count() < ReadCount)
                    {
                        break;
                    }
                    before = elements.Last().ExecutionId;
                }
                observer.OnCompleted();
            });});
        }

        IObservable<IBfExecution> CreateReaderSource(int before, int after)
        {
            Debug.WriteLine($"{nameof(ExecutionCachedSource)}.CreateReaderSource entered Before={before} After={after}");
            if (before == 0 || after == 0)
            {
                throw new ArgumentException();
            }

            return Observable.Create<IBfExecution>(observer => { return Task.Run(() =>
            {
                Debug.WriteLine($"{nameof(ExecutionCachedSource)}.CreateReaderSource subscribed Before={before} After={after}");

                var ticks = 0;
#if DEBUG
                var last = 0;
#endif
                // IEnumerable.ToObservable() has bug that is never completed if subscriber sends completed.
                foreach (var tick in _cache.GetBackwardExecutions(before, after))
                {
                    if (_cancel.IsCancellationRequested)
                    {
#if DEBUG
                        Debug.WriteLine($"{nameof(ExecutionCachedSource)}.CreateReaderSource unsubscribed and completed Before={before} Last={last} Ticks={ticks}");
#endif
                        observer.OnCompleted();
                        return;
                    }
#if DEBUG
                    last = tick.ExecutionId;
#endif
                    ticks++;
                    observer.OnNext(tick);
                }
#if DEBUG
                Debug.WriteLine($"{nameof(ExecutionCachedSource)}.CreateReaderSource reached to end and completed Before={before} Last={last} Ticks={ticks}");
#endif
                observer.OnCompleted();
            });});
        }

        IObservable<IBfExecution> CreateSimpleCopySource(int before, int after)
        {
            Debug.WriteLine($"{nameof(ExecutionCachedSource)}.CreateSimpleCopySource entered Before={before} After={after}");
            return Observable.Create<IBfExecution>(observer =>
            {
                Debug.WriteLine($"{nameof(ExecutionCachedSource)}.CreateSimpleCopySource subscribed Before={before} After={after}");
#if DEBUG
                var last = 0;
#endif
                return GetExecutions(before, after).Subscribe(
                    tick =>
                    {
#if DEBUG
                        last = tick.ExecutionId;
#endif
                        _cache.Add(tick);
                        observer.OnNext(tick);
                    },
                    ex => { observer.OnError(ex); },
                    () =>
                    {
                        if (_cache.CurrentBlockTicks == 0)
                        {
                            if (before != 0 && after != 0)
                            {
                                _cache.InsertGap(before, after);
                            }
                        }
                        else
                        {
                            _cache.SaveChanges();
                        }
#if DEBUG
                        Debug.WriteLine($"{nameof(ExecutionCachedSource)}.CreateSimpleCopySource completed Before={before} Last={last} Ticks={_cache.CurrentBlockTicks}");
#endif
                        observer.OnCompleted();
                    }
                ).AddTo(_disposable);
            });
        }

        IObservable<IBfExecution> CreateMergedSource(List<IManageRecord> manageRecords, int before)
        {
            Debug.WriteLine($"{nameof(ExecutionCachedSource)}.CreateMergedSource entered Before={before}");
            var histObservables = new List<IObservable<IBfExecution>>();
            int skipCount = 0;
            if (before == 0)
            {
                histObservables.Add(CreateSimpleCopySource(0, manageRecords[0].EndExecutionId));
            }
            else if (before <= manageRecords[0].EndExecutionId + 1) // start point is on cache
            {
                skipCount = 1;
                for (int index = 0; index < manageRecords.Count; index++, skipCount++)
                {
                    var block = manageRecords[index];
                    if (before <= block.EndExecutionId + 1 && before > block.StartExecutionId)
                    {
                        histObservables.Add(CreateReaderSource(before, block.StartExecutionId - 1));
                        before = block.StartExecutionId;
                        break;
                    }
                }
            }

            foreach (var block in manageRecords.Skip(skipCount))
            {
                if (before > block.EndExecutionId + 1)
                {
                    histObservables.Add(CreateSimpleCopySource(before, block.EndExecutionId));
                }

                histObservables.Add(CreateReaderSource(block.EndExecutionId + 1, block.StartExecutionId - 1));
                before = block.StartExecutionId;
            }

            histObservables.Add(CreateSimpleCopySource(before, 0));

            return histObservables.Concat();
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }

        public IDisposable Subscribe(IObserver<IBfExecution> observer)
        {
            Debug.WriteLine($"{nameof(ExecutionCachedSource)} subscribed.");
#if DEBUG
            var last = 0;
#endif
            _source.Subscribe(
                tick =>
                {
#if DEBUG
                    last = tick.ExecutionId;
#endif
                    _cache.UpdateMarker(tick);
                    observer.OnNext(tick);
                },
                ex => { observer.OnError(ex); },
                () =>
                {
                    _cache.SaveChanges();
#if DEBUG
                    Debug.WriteLine($"{nameof(ExecutionCachedSource)} Last tick ID={last}");
#endif
                    Debug.WriteLine($"{nameof(ExecutionCachedSource)} completed.");
                    observer.OnCompleted();
                }
            ).AddTo(_disposable);

            return Disposable.Create(() =>
            {
                _cancel.Cancel();
                //observer.OnCompleted();
            });
        }
    }
}
