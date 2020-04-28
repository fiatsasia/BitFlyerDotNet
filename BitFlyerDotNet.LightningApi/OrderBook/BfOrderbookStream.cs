//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Reactive.Linq;

namespace BitFlyerDotNet.LightningApi
{
    internal class BfOrderBookStream : IObservable<BfOrderBook>
    {
        IObservable<BfOrderBook> _source;

        public BfOrderBookStream(RealtimeBoardSnapshotSource snapshot, RealtimeBoardSource update)
        {
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
            return _source.Subscribe(observer);
        }
    }
}
