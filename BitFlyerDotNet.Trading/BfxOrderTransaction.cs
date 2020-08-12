//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System.Diagnostics;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public abstract class BfxOrderTransaction : IBfxOrderTransaction
    {
        public abstract string? Id { get; }
        public BfxOrderTransactionState State { get; protected set; } = BfxOrderTransactionState.Idle;
        public abstract BfxOrderState OrderState { get; }

        public abstract void OnChildOrderEvent(BfChildOrderEvent coe);

        protected abstract void SendCancelOrderRequestAsync();
        protected abstract void CancelTransaction();

        string _derived;

        public BfxOrderTransaction()
        {
            _derived = GetType().Name == nameof(BfxParentOrderTransaction) ? "Parent" : "Child";
        }

        public virtual void Cancel()
        {
            switch (State)
            {
                case BfxOrderTransactionState.Idle:
                    if (OrderState == BfxOrderState.Ordered || OrderState == BfxOrderState.Executing)
                    {
                        SendCancelOrderRequestAsync();
                    }
                    break;

                case BfxOrderTransactionState.SendingOrder:
                    CancelTransaction();
                    break;

                case BfxOrderTransactionState.WaitingOrderAccepted:
                    SendCancelOrderRequestAsync();
                    break;

                case BfxOrderTransactionState.SendingCancel:
                    CancelTransaction();
                    break;

                case BfxOrderTransactionState.CancelAccepted:
                    break;
            }
        }

        protected virtual void ChangeState(BfxOrderTransactionState state)
        {
            Debug.WriteLine($"{_derived} transaction state changed: {State} -> {state}");
            State = state;
        }
    }
}
