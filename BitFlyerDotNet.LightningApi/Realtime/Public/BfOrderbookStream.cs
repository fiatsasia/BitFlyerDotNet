//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Reactive.Linq;
using System.Reactive.Disposables;

namespace BitFlyerDotNet.LightningApi
{
    internal class BfOrderBookStream : IObservable<BfOrderBook>
    {
        public readonly string ProductCode;
        IObservable<BfOrderBook> _source;
        IDisposable _disposable;
        Action<BfOrderBookStream> _dispose;

        public BfOrderBookStream(string productCode, RealtimeBoardSnapshotSource snapshot, RealtimeBoardSource update, Action<BfOrderBookStream> dispose)
        {
            ProductCode = productCode;
            _dispose = dispose;

            _source = Observable.Create<BfOrderBook>(observer =>
            {
                var orderBook = new BfOrderBook();
                var disposable =
                    snapshot.Select(e => (orders: e, isreset: true))
                    .Merge(
                        update.Select(e => (orders: e, isreset: false))
                    )
                .Subscribe(e =>
                {
                    if (e.isreset)
                    {
                        orderBook.Reset(e.orders);
                    }
                    else
                    {
                        orderBook.UpdateDelta(e.orders);
                    }

                    observer.OnNext(orderBook);
                });

                return () => { disposable.Dispose(); };
            });
        }

        public IDisposable Subscribe(IObserver<BfOrderBook> observer)
        {
            _disposable = _source.Subscribe(observer);
            return Disposable.Create(OnDispose);
        }

        void OnDispose()
        {
            _disposable.Dispose();
            _dispose(this);
        }
    }
}
