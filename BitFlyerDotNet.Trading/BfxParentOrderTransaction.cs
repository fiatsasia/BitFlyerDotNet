//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxParentOrderTransaction : BfxOrderTransaction, IBfxParentOrderTransaction
    {
        public override string? Id => Order.AcceptanceId;
        public BfxParentOrder Order { get; private set; }
        public override BfxOrderState OrderState => Order.State;

        protected override void CancelTransaction() => _cts.Cancel();

        // Events
        EventHandler<BfxOrderTransactionEventArgs>? OrderTransactionEvent;

        // Private properties
        BfxMarket _market;
        CancellationTokenSource _cts = new CancellationTokenSource();

        public BfxParentOrderTransaction(BfxMarket market, BfxParentOrder order, EventHandler<BfxOrderTransactionEventArgs> handler)
        {
            _market = market;
            Order = order;
            OrderTransactionEvent = handler;
        }

        // - 経過時間でリトライ終了のオプション
        public async Task SendOrderRequestAsync()
        {
            if (Order.Request == null)
            {
                throw new BitFlyerDotNetException();
            }

            try
            {
                ChangeState(BfxOrderTransactionState.SendingOrder);
                NotifyEvent(BfxOrderTransactionEventType.OrderSending);
                for (var retry = 0; retry <= _market.Config.OrderRetryMax; retry++)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    var resp = await _market.Client.SendParentOrderAsync(Order.Request, _cts.Token);
                    if (!resp.IsError)
                    {
                        Order.Update(resp.GetContent());
                        ChangeState(BfxOrderTransactionState.WaitingOrderAccepted);
                        NotifyEvent(BfxOrderTransactionEventType.OrderSent, _market.ServerTime, resp);
                        _market.RegisterTransaction(this);
                        return;
                    }

                    Debug.WriteLine($"SendParentOrder failed: {resp.StatusCode} {resp.ErrorMessage}");
                    _cts.Token.ThrowIfCancellationRequested();
                    Debug.WriteLine("Trying retry...");
                    await Task.Delay(_market.Config.OrderRetryInterval);
                }

                Debug.WriteLine("SendOrderRequest - Retried out");
                ChangeState(BfxOrderTransactionState.Idle);
                NotifyEvent(BfxOrderTransactionEventType.OrderSendFailed);
                throw new BitFlyerDotNetException();
            }
            catch (OperationCanceledException ex)
            {
                Debug.WriteLine("SendParentOrderRequestAsync is canceled");
                ChangeState(BfxOrderTransactionState.Idle);
                NotifyEvent(BfxOrderTransactionEventType.OrderSendCanceled, _market.ServerTime, ex);
            }
        }

        protected override async void SendCancelOrderRequestAsync()
        {
            ChangeState(BfxOrderTransactionState.SendingCancel);
            NotifyEvent(BfxOrderTransactionEventType.CancelSending);
            try
            {
                _cts.Token.ThrowIfCancellationRequested();
                var resp = await _market.Client.CancelParentOrderAsync(_market.ProductCode, string.Empty, Order.ParentOrderAcceptanceId, _cts.Token);
                if (!resp.IsError)
                {
                    ChangeState(BfxOrderTransactionState.CancelAccepted);
                    NotifyEvent(BfxOrderTransactionEventType.CancelSent, _market.ServerTime, resp);
                }
                else
                {
                    ChangeState(BfxOrderTransactionState.Idle);
                    NotifyEvent(BfxOrderTransactionEventType.CancelSendFailed, _market.ServerTime, resp);
                }
            }
            catch (OperationCanceledException ex)
            {
                ChangeState(BfxOrderTransactionState.Idle);
                NotifyEvent(BfxOrderTransactionEventType.CancelSendCanceled, _market.ServerTime, ex);
            }
        }

        public void OnParentOrderEvent(BfParentOrderEvent poe)
        {
            if (poe.ParentOrderAcceptanceId != Order.ParentOrderAcceptanceId)
            {
                throw new ApplicationException();
            }

            Order.Update(poe);

            switch (poe.EventType)
            {
                case BfOrderEventType.Order: // Order accepted
                    ChangeState(BfxOrderTransactionState.Idle);
                    NotifyEvent(BfxOrderTransactionEventType.Ordered, poe);
                    break;

                case BfOrderEventType.OrderFailed:
                    ChangeState(BfxOrderTransactionState.Idle);
                    NotifyEvent(BfxOrderTransactionEventType.OrderFailed, poe);
                    break;

                case BfOrderEventType.Cancel:
                    ChangeState(BfxOrderTransactionState.Idle);
                    NotifyEvent(BfxOrderTransactionEventType.Canceled, poe);
                    break;

                case BfOrderEventType.CancelFailed:
                    ChangeState(BfxOrderTransactionState.Idle);
                    NotifyEvent(BfxOrderTransactionEventType.CancelFailed, poe);
                    break;

                case BfOrderEventType.Trigger:
                    break;

                case BfOrderEventType.Expire:
                    NotifyEvent(BfxOrderTransactionEventType.Expired, poe);
                    break;

                case BfOrderEventType.Complete:
                    if (Order.State == BfxOrderState.Completed)
                    {
                        NotifyEvent(BfxOrderTransactionEventType.Completed, poe);
                    }
                    break;

                case BfOrderEventType.Unknown:
                case BfOrderEventType.Execution:
                    throw new NotSupportedException();
            }
        }

        public override void OnChildOrderEvent(BfChildOrderEvent coe)
        {
            var childOrderIndex = Order.Update(coe);
            var childOrder = Order.Children[childOrderIndex];

            switch (coe.EventType)
            {
                case BfOrderEventType.Order:
                    NotifyChildOrderEvent(BfxOrderTransactionEventType.Ordered, childOrderIndex, coe);
                    break;

                case BfOrderEventType.OrderFailed:
                    NotifyChildOrderEvent(BfxOrderTransactionEventType.OrderFailed, childOrderIndex, coe);
                    break;

                case BfOrderEventType.Cancel:
                    NotifyChildOrderEvent(BfxOrderTransactionEventType.Canceled, childOrderIndex, coe);
                    break;

                case BfOrderEventType.CancelFailed:
                    if (coe.ChildOrderType == BfOrderType.Unknown)
                    {
                        Debug.WriteLine("Dirty Cancel Failed event received.");
                        break; // Skip "dirty" event See https://scrapbox.io/BitFlyerDotNet/ChildOrderEvent
                    }
                    NotifyChildOrderEvent(BfxOrderTransactionEventType.CancelFailed, childOrderIndex, coe);
                    break;

                case BfOrderEventType.Execution:
                    NotifyChildOrderEvent(childOrder.State == BfxOrderState.Executing ? BfxOrderTransactionEventType.PartiallyExecuted : BfxOrderTransactionEventType.Executed, childOrderIndex, coe);
                    break;

                case BfOrderEventType.Expire:
                    NotifyChildOrderEvent(BfxOrderTransactionEventType.Expired, childOrderIndex, coe);
                    break;

                case BfOrderEventType.Unknown:
                case BfOrderEventType.Complete:
                case BfOrderEventType.Trigger: // Not happened when Simple Order ?
                    throw new NotSupportedException();
            }
        }

        void NotifyEvent(BfxOrderTransactionEventType oet, DateTime time, object? parameter)
        {
            OrderTransactionEvent?.Invoke(this, new BfxOrderTransactionEventArgs(Order)
            {
                EventType = oet,
                State = State,
                Time = time,
                Parameter = parameter,
            });
        }
        void NotifyEvent(BfxOrderTransactionEventType oet) => NotifyEvent(oet, _market.ServerTime, null);
        void NotifyEvent(BfxOrderTransactionEventType oet, BfParentOrderEvent poe) => NotifyEvent(oet, poe.EventDate, poe);

        void NotifyChildOrderEvent(BfxOrderTransactionEventType oet, int childOrderIndex, BfChildOrderEvent coe)
        {
            if (Order.Children.Length == 1)
            {
                OrderTransactionEvent?.Invoke(this, new BfxOrderTransactionEventArgs(Order.Children[0])
                {
                    EventType = oet,
                    State = State,
                    Time = coe.EventDate,
                    Parameter = coe,
                });
            }
            else
            {
                OrderTransactionEvent?.Invoke(this, new BfxOrderTransactionEventArgs(Order)
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
    }
}
