//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Diagnostics;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public abstract class BfxOrderTransaction : IBfxOrderTransaction
    {
        public Guid Id { get; }
        public DateTime OpenTime { get; }

        public abstract string MarketId { get; }
        public BfxOrderTransactionState State { get; protected set; } = BfxOrderTransactionState.Idle;
        public abstract BfxOrderState OrderState { get; }
        public abstract IBfxOrder Order { get; }
        public virtual bool HasParent { get; } = false;

        public abstract void OnChildOrderEvent(BfChildOrderEvent coe);

        protected BfxMarket Market { get; private set; }
        protected abstract void SendCancelOrderRequestAsync();
        protected abstract void CancelTransaction();

        string _derived;

        public BfxOrderTransaction(BfxMarket market)
        {
            Market = market;
            Id = Guid.NewGuid();
            OpenTime = market.ServerTime;
            _derived = GetType().Name == nameof(BfxParentOrderTransaction) ? "Parent" : "Child";
        }

        public bool IsCancelable
        {
            get
            {
                switch (State)
                {
                    case BfxOrderTransactionState.Idle:
                        if (OrderState == BfxOrderState.Ordered || OrderState == BfxOrderState.Executing)
                        {
                            return true;
                        }
                        break;

                    case BfxOrderTransactionState.SendingOrder:
                        return true;

                    case BfxOrderTransactionState.WaitingOrderAccepted:
                        return true;

                    case BfxOrderTransactionState.SendingCancel:
                        return true;
                }
                return false;
            }
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
            }
        }

        protected virtual void ChangeState(BfxOrderTransactionState state)
        {
            Debug.WriteLine($"{_derived} transaction state changed: {State} -> {state}");
            State = state;
        }

        protected void NotifyEvent(BfxOrderTransactionEventType oet, DateTime time, object? parameter)
        {
            Market.InvokeOrderTransactionEvent(this, new BfxOrderTransactionEventArgs(Order)
            {
                EventType = oet,
                State = State,
                Time = time,
                Parameter = parameter,
            });
        }

        protected void NotifyEvent(BfxOrderTransactionEventType oet) => NotifyEvent(oet, Market.ServerTime, null);

        protected void NotifyChildOrderEvent(BfxOrderTransactionEventType oet, int childOrderIndex, BfChildOrderEvent coe)
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
                    EventType = BfxOrderTransactionEventType.ChildOrderEvent,
                    State = State,
                    Time = coe.EventDate,
                    Parameter = coe,
                    ChildEventType = oet,
                    ChildOrderIndex = childOrderIndex,
                });
            }
        }

        protected void NotifyEvent(BfxOrderTransactionEventType oet, BfChildOrderEvent coe) => NotifyEvent(oet, coe.EventDate, coe);
        protected void NotifyEvent(BfxOrderTransactionEventType oet, BfParentOrderEvent poe) => NotifyEvent(oet, poe.EventDate, poe);
    }
}
