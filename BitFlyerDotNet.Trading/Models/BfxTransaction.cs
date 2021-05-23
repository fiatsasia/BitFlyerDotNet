//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Threading.Tasks;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public abstract class BfxTransaction : IBfxTransaction
    {
        public Guid Id { get; }
        public DateTime OpenTime { get; }

        public abstract string MarketId { get; }
        public BfxTransactionState State { get; protected set; } = BfxTransactionState.Idle;
        public abstract BfxOrderState OrderState { get; }
        public abstract IBfxOrder Order { get; }
        public virtual bool HasParent { get; } = false;

        public abstract void OnChildOrderEvent(BfChildOrderEvent coe);

        protected BfxMarket Market { get; private set; }
        protected abstract Task SendCancelOrderRequestAsync();
        protected abstract void CancelTransaction();

        string _derived;

        public BfxTransaction(BfxMarket market)
        {
            Market = market;
            Id = Guid.NewGuid();
            OpenTime = market.ServerTime;
            _derived = GetType().Name == nameof(BfxParentTransaction) ? "Parent" : "Child";
        }

        public bool IsCancelable
        {
            get
            {
                switch (State)
                {
                    case BfxTransactionState.Idle:
                        if (OrderState == BfxOrderState.Ordered || OrderState == BfxOrderState.PartiallyExecuted)
                        {
                            return true;
                        }
                        break;

                    case BfxTransactionState.SendingOrder:
                        return true;

                    case BfxTransactionState.WaitingOrderAccepted:
                        return true;

                    case BfxTransactionState.SendingCancel:
                        return true;
                }
                return false;
            }
        }

        public virtual void Cancel()
        {
            switch (State)
            {
                case BfxTransactionState.Idle:
                    if (OrderState == BfxOrderState.Ordered || OrderState == BfxOrderState.PartiallyExecuted)
                    {
                        SendCancelOrderRequestAsync();
                    }
                    break;

                case BfxTransactionState.SendingOrder:
                    CancelTransaction();
                    break;

                case BfxTransactionState.WaitingOrderAccepted:
                    SendCancelOrderRequestAsync();
                    break;

                case BfxTransactionState.SendingCancel:
                    CancelTransaction();
                    break;
            }
        }

        protected virtual void ChangeState(BfxTransactionState state)
        {
            Log.Trace($"{_derived} transaction state changed: {State} -> {state}");
            State = state;
        }

        protected void NotifyEvent(BfxTransactionEventType oet, DateTime time, object? parameter)
        {
            Market.InvokeOrderTransactionEvent(this, new BfxOrderTransactionEventArgs(Order)
            {
                EventType = oet,
                State = State,
                Time = time,
                Parameter = parameter,
            });
        }

        protected void NotifyEvent(BfxTransactionEventType oet) => NotifyEvent(oet, Market.ServerTime, null);

        protected void NotifyChildOrderEvent(BfxTransactionEventType oet, int childOrderIndex, BfChildOrderEvent coe)
        {
            if (Order.Children.Length == 1)
            {
                Market.InvokeOrderTransactionEvent(this, new BfxOrderTransactionEventArgs(Order.Children[0])
                {
                    EventType = oet,
                    State = State,
                    Time = coe.EventDate,
                    Parameter = coe,
                });
            }
            else
            {
                Market.InvokeOrderTransactionEvent(this, new BfxOrderTransactionEventArgs(Order)
                {
                    EventType = BfxTransactionEventType.ChildOrderEvent,
                    State = State,
                    Time = coe.EventDate,
                    Parameter = coe,
                    ChildEventType = oet,
                    ChildOrderIndex = childOrderIndex,
                });
            }
        }

        protected void NotifyEvent(BfxTransactionEventType oet, BfChildOrderEvent coe) => NotifyEvent(oet, coe.EventDate, coe);
        protected void NotifyEvent(BfxTransactionEventType oet, BfParentOrderEvent poe) => NotifyEvent(oet, poe.EventDate, poe);
    }
}
