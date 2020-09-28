//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using BitFlyerDotNet.LightningApi;
using System.Linq;

namespace BitFlyerDotNet.Trading
{
    public class BfxChildOrderTransaction : BfxOrderTransaction
    {
        // Public properties
        public override string MarketId => _order.ChildOrderAcceptanceId;
        public override IBfxOrder Order => _order;
        public override BfxOrderState OrderState => Order.State;
        public BfxParentOrderTransaction? Parent { get; }
        public override bool HasParent => Parent != null;

        protected override void CancelTransaction() => _cts.Cancel();

        // Private properties
        CancellationTokenSource _cts = new CancellationTokenSource();
        BfxChildOrder _order;

        public BfxChildOrderTransaction(BfxMarket market, BfxChildOrder order, BfxParentOrderTransaction parent)
            : base(market)
        {
            _order = order;
            Parent = parent;
        }

        public BfxChildOrderTransaction(BfxMarket market, BfxChildOrder order)
            : base(market)
        {
            _order = order;
            Market.RealtimeSource.ConnectionSuspended += OnRealtimeConnectionSuspended;
            Market.RealtimeSource.ConnectionResumed += OnRealtimeConnectionResumed;
        }

        void OnRealtimeConnectionSuspended()
        {
        }

        void OnRealtimeConnectionResumed()
        {
            if (string.IsNullOrEmpty(_order.ChildOrderAcceptanceId))
            {
                return;
            }
            var order = Market.Client.GetChildOrders(Market.ProductCode, childOrderAcceptanceId: _order.ChildOrderAcceptanceId).GetContent().FirstOrDefault();
            if (order == null)
            {
                return;
            }

            Debug.WriteLine($"{DateTime.Now} Found standalone order. {order.ChildOrderType} {order.ChildOrderState}");
            var oldExecSize = Order.ExecutedSize.HasValue ? Order.ExecutedSize.Value : 0m;
            _order.Update(order);

            if (order.ExecutedSize > oldExecSize)
            {
                Debug.WriteLine($"{DateTime.Now} Found additional execs. size:{order.ExecutedSize}");
                var execs = Market.Client.GetPrivateExecutions(Market.ProductCode, childOrderAcceptanceId: order.ChildOrderAcceptanceId).GetContent();
                _order.Update(execs);
            }
        }

        // - 経過時間でリトライ終了のオプション
        public async Task SendOrderRequestAsync()
        {
            if (_order.Request == null)
            {
                throw new BitFlyerDotNetException();
            }

            try
            {
                ChangeState(BfxOrderTransactionState.SendingOrder);
                NotifyEvent(BfxOrderTransactionEventType.OrderSending);
                for (var retry = 0; retry <= Market.Config.OrderRetryMax; retry++)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    var resp = await Market.Client.SendChildOrderAsync(_order.Request, _cts.Token);
                    if (!resp.IsError)
                    {
                        _order.Update(resp.GetContent());
                        ChangeState(BfxOrderTransactionState.WaitingOrderAccepted);
                        NotifyEvent(BfxOrderTransactionEventType.OrderSent, Market.ServerTime, resp);
                        Market.RegisterTransaction(this);
                        return;
                    }

                    Debug.WriteLine($"SendChildOrder failed: {resp.StatusCode} {resp.ErrorMessage}");
                    _cts.Token.ThrowIfCancellationRequested();
                    Debug.WriteLine("Trying retry...");
                    await Task.Delay(Market.Config.OrderRetryInterval);
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
                NotifyEvent(BfxOrderTransactionEventType.OrderSendCanceled, Market.ServerTime, ex);
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
                var resp = await Market.Client.CancelChildOrderAsync(Market.ProductCode, string.Empty, _order.ChildOrderAcceptanceId, _cts.Token);
                if (!resp.IsError)
                {
                    ChangeState(BfxOrderTransactionState.CancelAccepted);
                    NotifyEvent(BfxOrderTransactionEventType.CancelSent, Market.ServerTime, resp);
                }
                else
                {
                    ChangeState(BfxOrderTransactionState.Idle);
                    NotifyEvent(BfxOrderTransactionEventType.CancelSendFailed, Market.ServerTime, resp);
                }
            }
            catch (OperationCanceledException ex)
            {
                ChangeState(BfxOrderTransactionState.Idle);
                NotifyEvent(BfxOrderTransactionEventType.CancelSendCanceled, Market.ServerTime, ex);
            }
        }

        // Call from BfxMarket
        public override void OnChildOrderEvent(BfChildOrderEvent coe)
        {
            if (coe.ChildOrderAcceptanceId != _order.ChildOrderAcceptanceId)
            {
                throw new ArgumentException();
            }

            _order.Update(coe);

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

        protected override void ChangeState(BfxOrderTransactionState state)
        {
            base.ChangeState(state);
            if (state == BfxOrderTransactionState.Closed)
            {
                Market.RealtimeSource.ConnectionSuspended -= OnRealtimeConnectionSuspended;
                Market.RealtimeSource.ConnectionResumed -= OnRealtimeConnectionResumed;
            }
        }
    }
}
