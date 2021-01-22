//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Disposables;
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

            Log.Trace($"{nameof(ExecutionCachedSource)} constructed Before={before}");

            var manageRecords = _cache.GetManageTable();
            _source = (manageRecords.Count == 0) ? CreateSimpleCopySource(before, 0) : CreateMergedSource(manageRecords, before);
        }

        const int ReadCount = 500;
        int ReadInterval = 3000; // Public API is limited 500 requests in a minute.
        IObservable<IBfExecution> GetExecutions(int before, int after)
        {
            return Observable.Create<IBfExecution>(observer => { return Task.Run(async () =>
            {
                while (true)
                {
                    var result = await _client.GetExecutionsAsync(_productCode, ReadCount, before, after);
                    if (result.IsError)
                    {
                        Thread.Sleep(ReadInterval);
                        continue;
                    }
                    Thread.Sleep(100);

                    var elements = result.GetContent();
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
            Log.Trace($"{nameof(ExecutionCachedSource)}.CreateReaderSource entered Before={before} After={after}");
            if (before == 0 || after == 0)
            {
                throw new ArgumentException();
            }

            return Observable.Create<IBfExecution>(observer => { return Task.Run(() =>
            {
                Log.Trace($"{nameof(ExecutionCachedSource)}.CreateReaderSource subscribed Before={before} After={after}");

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
                        Log.Trace($"{nameof(ExecutionCachedSource)}.CreateReaderSource unsubscribed and completed Before={before} Last={last} Ticks={ticks}");
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
                Log.Trace($"{nameof(ExecutionCachedSource)}.CreateReaderSource reached to end and completed Before={before} Last={last} Ticks={ticks}");
#endif
                observer.OnCompleted();
            });});
        }

        IObservable<IBfExecution> CreateSimpleCopySource(int before, int after)
        {
            Log.Trace($"{nameof(ExecutionCachedSource)}.CreateSimpleCopySource entered Before={before} After={after}");
            return Observable.Create<IBfExecution>(observer =>
            {
                Log.Trace($"{nameof(ExecutionCachedSource)}.CreateSimpleCopySource subscribed Before={before} After={after}");
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
                        Log.Trace($"{nameof(ExecutionCachedSource)}.CreateSimpleCopySource completed Before={before} Last={last} Ticks={_cache.CurrentBlockTicks}");
#endif
                        observer.OnCompleted();
                    }
                ).AddTo(_disposable);
            });
        }

        IObservable<IBfExecution> CreateMergedSource(List<IManageRecord> manageRecords, int before)
        {
            Log.Trace($"{nameof(ExecutionCachedSource)}.CreateMergedSource entered Before={before}");
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
            Log.Trace($"{nameof(ExecutionCachedSource)} subscribed.");
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
                    Log.Trace($"{nameof(ExecutionCachedSource)} Last tick ID={last}");
#endif
                    Log.Trace($"{nameof(ExecutionCachedSource)} completed.");
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
