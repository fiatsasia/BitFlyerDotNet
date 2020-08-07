//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public abstract class BfxOrderTransaction : IBfxOrderTransaction
    {
        public BfxOrderTransactionState State { get; protected set; } = BfxOrderTransactionState.Idle;
        public abstract BfxOrderState OrderState { get; }

        public abstract void OnChildOrderEvent(BfChildOrderEvent coe);

        protected abstract Task<IBitFlyerResponse> SendCancelOrderRequestAsync();
        protected abstract void TryAbortSendingOrder();

        public virtual void CancelOrder()
        {
            switch (State)
            {
                case BfxOrderTransactionState.Idle:
                    if (OrderState == BfxOrderState.Ordered || OrderState == BfxOrderState.Executing)
                    {
                        SendCancelOrderRequestAsync();
                    }
                    break;

                case BfxOrderTransactionState.WaitingOrderAccepted:
                case BfxOrderTransactionState.OrderAccepted:
                    SendCancelOrderRequestAsync();
                    break;

                case BfxOrderTransactionState.SendingCancel:
                case BfxOrderTransactionState.CancelAccepted:
                case BfxOrderTransactionState.WaitingCancelCompleted:
                    break;

                case BfxOrderTransactionState.SendingOrder:
                    TryAbortSendingOrder();
                    break;
            }
        }

        protected virtual void ChangeState(BfxOrderTransactionState state)
        {
            State = state;
        }

    }
}
