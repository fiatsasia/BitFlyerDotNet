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
    public class BfxChildOrderTransaction : IBfOrderTransaction, IDisposable
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

        public BfxChildOrderTransactionState State { get; private set; }
        BfTradingMarket _market;
        CompositeDisposable _disposables = new CompositeDisposable();
        ReaderWriterLockSlim _stateLock = new ReaderWriterLockSlim();

        BitFlyerClient Client => _market.Client;
        RealtimeSourceFactory RealtimeSource => _market.RealtimeSource;
        BfTradingMarketConfiguration Config => _market.Config;

        public event EventHandler<BfxChildOrderTransactionEventArgs> StateChanged
        {
            add { State.StateChanged += value; }
            remove { State.StateChanged -= value; }
        }

        public event EventHandler<BfxChildOrderEventArgs> OrderChanged
        {
            add { State.OrderChanged += value; }
            remove { State.OrderChanged -= value; }
        }

        public BfxChildOrderTransaction(BfTradingMarket market, BfChildOrderRequest request)
        {
            DebugEx.EnterMethod();
            _market = market;
            State = new BfxChildOrderTransactionState(_market, request);
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
                if (!State.OnOrderRequested())
                {
                    DebugEx.Trace();
                    return false;
                }

                // Start monitoring first
                DebugEx.Trace();
                RealtimeSource.GetExecutionSource(State.ProductCode)
                    .Where(exec => exec.ChildOrderAcceptanceId == State.AcceptanceId)
                    .Subscribe(OnExecutionTicked).AddTo(_disposables);

                DebugEx.Trace();
                State.OnOrderAccepted(Client.SendChildOrder(State.Request).GetResult());

                DebugEx.Trace();
                Observable.Timer(Config.ChildOrderConfirmDelay, Config.ChildOrderConfirmInterval).Subscribe(count => ConfirmOrder()).AddTo(_disposables);
                return true;
            }
            catch (Exception ex)
            {
                DebugEx.Trace(ex.Message);
                _disposables.Dispose();
                State.OnOrderFailed(ex);
                return false;
            }
            finally
            {
                DebugEx.Trace();
                _stateLock.ExitWriteLock();
                DebugEx.ExitMethod();
            }
        }

        void OnTransactionCompleted()
        {
            _disposables.Dispose();
        }

        void OnExecutionTicked(BfExecution exec)
        {
            try
            {
                DebugEx.EnterMethod();
                _stateLock.EnterWriteLock();

                DebugEx.Trace();
                if (State.OnExecutionReceived(exec) && State.OrderState == BfOrderState.Completed)
                {
                    OnTransactionCompleted();
                }
            }
            finally
            {
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

                if (!State.OnCancelRequested())
                {
                    DebugEx.Trace();
                    return false;
                }

                DebugEx.Trace();
                State.OnCancelAccepted(Client.CancelChildOrder(State.ProductCode, childOrderAcceptanceId: State.AcceptanceId).GetResult());
                return true;
            }
            catch
            {
                DebugEx.Trace();
                State.OnCancelFailed();
                return false;
            }
            finally
            {
                DebugEx.Trace();
                _stateLock.ExitWriteLock();
                DebugEx.ExitMethod();
            }
        }

        void ConfirmOrder()
        {
            try
            {
                //DebugEx.EnterMethod();
                _stateLock.EnterWriteLock();

                // Get order information
                //DebugEx.Trace();
                State.OnOrderConfirmed(Client.GetChildOrders(State.ProductCode, childOrderAcceptanceId: State.AcceptanceId).GetResult());

                //DebugEx.Trace();
                if (State.OrderState != BfOrderState.Active && State.OrderState != BfOrderState.Unknown)
                {
                    //DebugEx.Trace();
                    OnTransactionCompleted();
                }
            }
            finally
            {
                //DebugEx.Trace();
                _stateLock.ExitWriteLock();
                //DebugEx.ExitMethod();
            }
        }
    }
}
