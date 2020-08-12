//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using BitFlyerDotNet.LightningApi;
using System.Runtime.InteropServices.ComTypes;

namespace BitFlyerDotNet.Trading
{
    public class BfxChildOrderTransaction : BfxOrderTransaction, IBfxChildOrderTransaction
    {
        // Public properties
        public override string? Id => Order.AcceptanceId;
        public BfxChildOrder Order { get; private set; }
        public override BfxOrderState OrderState => Order.State;
        public BfxParentOrderTransaction? Parent { get; }

        protected override void CancelTransaction() => _cts.Cancel();

        // Events
        EventHandler<BfxOrderTransactionEventArgs> OrderTransactionEvent;

        // Private properties
        BfxMarket _market;
        CancellationTokenSource _cts = new CancellationTokenSource();

        public BfxChildOrderTransaction(BfxMarket market, BfxChildOrder order, BfxParentOrderTransaction? parent, EventHandler<BfxOrderTransactionEventArgs> handler)
        {
            _market = market;
            Order = order;
            Parent = parent;
            OrderTransactionEvent = handler;
        }

        public BfxChildOrderTransaction(BfxMarket market, BfxChildOrder order, EventHandler<BfxOrderTransactionEventArgs> handler)
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
                    var resp = await _market.Client.SendChildOrderAsync(Order.Request, _cts.Token);
                    if (!resp.IsError)
                    {
                        Order.Update(resp.GetContent());
                        ChangeState(BfxOrderTransactionState.WaitingOrderAccepted);
                        NotifyEvent(BfxOrderTransactionEventType.OrderSent, _market.ServerTime, resp);
                        _market.RegisterTransaction(this);
                        return;
                    }

                    Debug.WriteLine($"SendChildOrder failed: {resp.StatusCode} {resp.ErrorMessage}");
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
                Debug.WriteLine("SendChildOrderRequestAsync is canceled");
                ChangeState(BfxOrderTransactionState.Idle);
                NotifyEvent(BfxOrderTransactionEventType.OrderSendCanceled, _market.ServerTime, ex);
            }
        }

        // - エラーリトライ(無限リトライ)
        // - 注文執行によるキャンセルの中止 => CancelFailed受信
        // - 注文送信リトライ中のキャンセル
        protected override async void SendCancelOrderRequestAsync()
        {
            ChangeState(BfxOrderTransactionState.SendingCancel);
            NotifyEvent(BfxOrderTransactionEventType.CancelSending);
            try
            {
                _cts.Token.ThrowIfCancellationRequested();
                var resp = await _market.Client.CancelChildOrderAsync(_market.ProductCode, string.Empty, Order.ChildOrderAcceptanceId, _cts.Token);
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

        // Call from BfxMarket
        public override void OnChildOrderEvent(BfChildOrderEvent coe)
        {
            if (coe.ChildOrderAcceptanceId != Order.ChildOrderAcceptanceId)
            {
                throw new ArgumentException();
            }

            Order.Update(coe);

            switch (coe.EventType)
            {
                case BfOrderEventType.Order: // Order registered
                    ChangeState(BfxOrderTransactionState.Idle);
                    NotifyEvent(BfxOrderTransactionEventType.Ordered, coe);
                    break;

                case BfOrderEventType.OrderFailed:
                    ChangeState(BfxOrderTransactionState.Idle);
                    NotifyEvent(BfxOrderTransactionEventType.OrderFailed, coe);
                    break;

                case BfOrderEventType.Cancel:
                    ChangeState(BfxOrderTransactionState.Idle);
                    NotifyEvent(BfxOrderTransactionEventType.Canceled, coe);
                    break;

                case BfOrderEventType.CancelFailed:
                    ChangeState(BfxOrderTransactionState.Idle);
                    NotifyEvent(BfxOrderTransactionEventType.CancelFailed, coe);
                    break;

                case BfOrderEventType.Execution:
                    NotifyEvent(Order.State == BfxOrderState.Executing ? BfxOrderTransactionEventType.PartiallyExecuted : BfxOrderTransactionEventType.Executed, coe);
                    break;

                case BfOrderEventType.Expire:
                    NotifyEvent(BfxOrderTransactionEventType.Expired, coe);
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

        void NotifyEvent(BfxOrderTransactionEventType oet) =>  NotifyEvent(oet, _market.ServerTime, null);
        void NotifyEvent(BfxOrderTransactionEventType oet, BfChildOrderEvent coe) => NotifyEvent(oet, coe.EventDate, coe);
    }
}
