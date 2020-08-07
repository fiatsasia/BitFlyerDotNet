//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Threading.Tasks;
using System.Diagnostics;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxChildOrderTransaction : BfxOrderTransaction
    {
        // Public properties
        public BfxChildOrder Order { get; private set; }
        public override BfxOrderState OrderState => Order.State;
        public BfxParentOrderTransaction? Parent { get; }

        // Events
        public event EventHandler<BfxChildOrderTransactionEventArgs>? OrderTransactionEvent;

        // Private properties
        BfxMarket _market;

        public BfxChildOrderTransaction(BfxMarket market, BfxChildOrder order, BfxParentOrderTransaction? parent)
        {
            order.ApplyProductCode(market.ProductCode);
            _market = market;
            Order = order;
            Parent = parent;
        }

        public BfxChildOrderTransaction(BfxMarket market, BfxChildOrder order)
        {
            order.ApplyProductCode(market.ProductCode);
            _market = market;
            Order = order;
        }

        public void CancelTransaction()
        {
            // 注文送信中なら送信をキャンセル
            // 注文送信済みならキャンセルを送信
            SendCancelOrderRequestAsync();
        }

        // - 経過時間でリトライ終了のオプション
        // - 通信エラー以外でのリトライ終了
        // - 送信完了からChildOrderEvent(Order)までの状態
        public async Task<string> SendOrderRequestAsync()
        {
            Debug.Assert(State == BfxOrderTransactionState.Idle && Order.State == BfxOrderState.Outstanding);
            DebugEx.EnterMethod();
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
                    DebugEx.Trace();
                    var resp = await _market.Client.SendChildOrderAsync(Order.Request);
                    if (!resp.IsError)
                    {
                        Order.Update(resp.GetContent());
                        ChangeState(BfxOrderTransactionState.WaitingOrderAccepted);
                        NotifyEvent(BfxOrderTransactionEventType.OrderSent, resp);
                        return resp.GetContent().ChildOrderAcceptanceId;
                    }

                    DebugEx.Trace("Trying retry...");
                    await Task.Delay(_market.Config.OrderRetryInterval);
                }

                DebugEx.Trace("SendOrderRequest - Retried out");
                ChangeState(BfxOrderTransactionState.Idle);
                NotifyEvent(BfxOrderTransactionEventType.OrderSendFailed);
                throw new BitFlyerDotNetException();
            }
            catch (Exception ex)
            {
                DebugEx.Trace(ex.Message);
                throw;
            }
            finally
            {
                DebugEx.ExitMethod();
            }
        }

        protected override void TryAbortSendingOrder()
        {
            throw new NotImplementedException();
        }

        // - エラーリトライ(無限リトライ)
        // - 注文執行によるキャンセルの中止 => CancelFailed受信
        // - 注文送信リトライ中のキャンセル
        protected override async Task<IBitFlyerResponse> SendCancelOrderRequestAsync()
        {
            Debug.Assert(!string.IsNullOrEmpty(Order.ChildOrderAcceptanceId));
            Debug.Assert(State == BfxOrderTransactionState.Idle && Order.State == BfxOrderState.Ordered);

            DebugEx.EnterMethod();
            ChangeState(BfxOrderTransactionState.SendingCancel);
            NotifyEvent(BfxOrderTransactionEventType.CancelSending);
            try
            {
                DebugEx.Trace();
                var resp = await _market.Client.CancelChildOrderAsync(productCode: _market.ProductCode, childOrderAcceptanceId: Order.ChildOrderAcceptanceId);
                if (resp.IsError)
                {
                    ChangeState(BfxOrderTransactionState.Idle);
                    NotifyEvent(BfxOrderTransactionEventType.CancelSendFailed, resp);
                }
                else
                {
                    ChangeState(BfxOrderTransactionState.CancelAccepted);
                    NotifyEvent(BfxOrderTransactionEventType.CancelSent, resp);
                }
                return resp;
            }
            catch (Exception ex)
            {
                DebugEx.Trace(ex.Message);
                throw;
            }
            finally
            {
                DebugEx.Trace();
                DebugEx.ExitMethod();
            }
        }

        // Callback from BfxMarket
        public override void OnChildOrderEvent(BfChildOrderEvent coe)
        {
            if (coe.ChildOrderAcceptanceId != Order.ChildOrderAcceptanceId)
            {
                return; // Event for childrent of parent
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

                // - When TimeInForce=FOK
                case BfOrderEventType.Expire:
                    NotifyEvent(BfxOrderTransactionEventType.Expired, coe);
                    break;

                case BfOrderEventType.Complete:
                case BfOrderEventType.Trigger: // Not happened when Simple Order ?
                case BfOrderEventType.Unknown:
                    throw new NotSupportedException();
            }
        }

        void NotifyEvent(BfxOrderTransactionEventType oet)
        {
            OrderTransactionEvent?.Invoke(this, new BfxChildOrderTransactionEventArgs
            {
                EventType = oet,
                State = State,
                Time = _market.ServerTime,
                Order = Order,
            });
        }

        void NotifyEvent(BfxOrderTransactionEventType oet, BfChildOrderEvent coe)
        {
            OrderTransactionEvent?.Invoke(this, new BfxChildOrderTransactionEventArgs
            {
                EventType = oet,
                State = State,
                Time = coe.EventDate,
                Order = Order,

                OrderEvent = coe,
            });
        }

        void NotifyEvent(BfxOrderTransactionEventType oet, IBitFlyerResponse resp)
        {
            OrderTransactionEvent?.Invoke(this, new BfxChildOrderTransactionEventArgs
            {
                EventType = oet,
                State = State,
                Time = _market.ServerTime,
                Order = Order,

                Response = resp,
            });
        }
    }
}
