//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Threading;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Financial.Extensions;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxParentOrderTransaction : IBfOrderTransaction, IDisposable
    {
        public bool IsCancelable()
        {
            try
            {
                _stateLock.EnterReadLock();
                return State.IsCancelable();
            }
            finally
            {
                _stateLock.ExitReadLock();
            }
        }

        public bool IsOrderable()
        {
            try
            {
                _stateLock.EnterReadLock();
                return State.IsOrderable();
            }
            finally
            {
                _stateLock.ExitReadLock();
            }
        }

        public object Tag { get; set; }

        public BfxParentOrderTransactionState State { get; private set; }
        BfTradingMarket _market;
        CompositeDisposable _disposables = new CompositeDisposable();
        ReaderWriterLockSlim _stateLock = new ReaderWriterLockSlim();

        BitFlyerClient Client => _market.Client;
        BfTradingMarketConfiguration Config => _market.Config;

        public event EventHandler<BfxChildOrderEventArgs> ChildOrderChanged
        {
            add { State.ChildOrderChanged += value; }
            remove { State.ChildOrderChanged -= value; }
        }

        public event EventHandler<BfxParentOrderTransactionEventArgs> ParentOrderStateChanged
        {
            add { State.StateChanged += value; }
            remove { State.StateChanged -= value; }
        }

        public event EventHandler<BfxParentOrderEventArgs> ParentOrderChanged
        {
            add { State.ParentOrderChanged += value; }
            remove { State.ParentOrderChanged -= value; }
        }

        public BfxParentOrderTransaction(BfTradingMarket market, BfParentOrderRequest request)
        {
            DebugEx.EnterMethod();
            _market = market;
            State = new BfxParentOrderTransactionState(_market, request);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        public bool SendOrder()
        {
            DebugEx.EnterMethod();
            try
            {
                DebugEx.Trace();
                _stateLock.EnterWriteLock();

                DebugEx.Trace();
                if (!State.OnParentOrderRequested())
                {
                    DebugEx.Trace();
                    return false;
                }

                DebugEx.Trace();
                State.OnParentOrderAccepted(Client.SendParentOrder(State.Request).GetResult());

                DebugEx.Trace();
                Observable.ToAsync(StartConfirmOrder)().Subscribe().AddTo(_disposables);
                return true;
            }
            catch (Exception ex)
            {
                DebugEx.Trace(ex.Message);
                _disposables.Dispose();
                State.OnParentOrderFailed(ex);
                return false;
            }
            finally
            {
                DebugEx.Trace();
                _stateLock.ExitWriteLock();
                DebugEx.ExitMethod();
            }
        }

        public bool CancelOrder()
        {
            try
            {
                DebugEx.EnterMethod();
                _stateLock.EnterWriteLock();

                if (!State.OnParentOrderCancelRequested())
                {
                    DebugEx.Trace();
                    return false;
                }

                DebugEx.Trace();
                State.OnParentOrderCancelAccepted(Client.CancelParentOrder(State.ProductCode, parentOrderAcceptanceId: State.AcceptanceId).GetResult());
                return true;
            }
            catch
            {
                DebugEx.Trace();
                State.OnParentOrderCancelFailed();
                return false;
            }
            finally
            {
                DebugEx.Trace();
                _stateLock.ExitWriteLock();
                DebugEx.ExitMethod();
            }
        }

        void StartConfirmOrder()
        {
            // Get ParentOrderDetail to get PagingId and ParentOrderId from ParentOrderAcceptanceId
            BfParentOrderDetail detail = null;
            while (true)
            {
                var resp = Client.GetParentOrder(State.ProductCode, parentOrderAcceptanceId: State.AcceptanceId);
                if (resp.IsErrorOrEmpty)
                {
                    Thread.Sleep(_market.Config.OrderRetryInterval);
                    continue;
                }
                detail = resp.GetResult();
                break;
            }

            _stateLock.EnterWriteLock();
            State.OnParentOrderConfirmed(detail);
            _stateLock.ExitWriteLock();

            // Get ParentOrder to get parent order state
            Observable.Timer(TimeSpan.Zero, Config.ParentOrderConfirmInterval).Subscribe(_ =>
            {
                try
                {
                    _stateLock.EnterWriteLock();
                    State.OnParentOrderConfirmed(Client.GetParentOrders(State.ProductCode, count: 1, before: State.PagingId + 1).GetResult());
                    if (State.ParentOrderState != BfOrderState.Active && State.ParentOrderState != BfOrderState.Unknown)
                    {
                        _disposables.Dispose();
                    }
                }
                catch (Exception ex) { DebugEx.Trace(ex.Message); }
                finally { _stateLock.ExitWriteLock(); }
            }).AddTo(_disposables);

            // Start child order monitoring
            Observable.Timer(TimeSpan.Zero, Config.ChildOrderConfirmInterval).Subscribe(_ =>
            {
                try
                {
                    _stateLock.EnterWriteLock();
                    State.OnChildOrderConfirmed(Client.GetChildOrders(State.ProductCode, parentOrderId: State.OrderId).GetResult()); // parent order acceptance ID is not allowed.
                }
                catch (Exception ex) { DebugEx.Trace(ex.Message); }
                finally { _stateLock.ExitWriteLock(); }
            }).AddTo(_disposables);
        }
    }
}
