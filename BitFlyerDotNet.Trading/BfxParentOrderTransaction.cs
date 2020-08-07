//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reactive.Disposables;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxParentOrderTransaction : BfxOrderTransaction
    {
        public BfxParentOrder Order { get; private set; }
        public override BfxOrderState OrderState => Order.State;

        // Events
        public event EventHandler<BfxParentOrderTransactionEventArgs>? OrderTransactionEvent;

        BfxMarket _market;

        public BfxParentOrderTransaction(BfxMarket market, BfxParentOrder order)
        {
            _market = market;
            Order = order;
        }

        public async Task<string> SendOrderRequestAsync()
        {
            DebugEx.EnterMethod();
            if (Order.Request == null)
            {
                throw new BitFlyerDotNetException();
            }

            Order.Request.Parameters.ForEach(e => { e.ProductCode = _market.ProductCode; });
            try
            {
                ChangeState(BfxOrderTransactionState.SendingOrder);
                NotifyEvent(BfxOrderTransactionEventType.OrderSending);
                for (var retry = 0; retry <= _market.Config.OrderRetryMax; retry++)
                {
                    DebugEx.Trace();
                    var resp = await _market.Client.SendParentOrderAsync(Order.Request);
                    if (!resp.IsError)
                    {
                        Order.Update(resp.GetContent());
                        ChangeState(BfxOrderTransactionState.WaitingOrderAccepted);
                        NotifyEvent(BfxOrderTransactionEventType.OrderSent, resp);
                        return resp.GetContent().ParentOrderAcceptanceId;
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
                throw; // Abort transaction
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

        protected override async Task<IBitFlyerResponse> SendCancelOrderRequestAsync()
        {
            if (string.IsNullOrEmpty(Order.ParentOrderAcceptanceId))
            {
                throw new InvalidOperationException("Not ordered.");
            }

            DebugEx.EnterMethod();
            ChangeState(BfxOrderTransactionState.SendingCancel);
            NotifyEvent(BfxOrderTransactionEventType.CancelSending);
            try
            {
                DebugEx.Trace();
                var resp = await _market.Client.CancelParentOrderAsync(productCode: _market.ProductCode, parentOrderAcceptanceId: Order.ParentOrderAcceptanceId);
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
            catch
            {
                DebugEx.Trace();
                throw;
            }
            finally
            {
                DebugEx.Trace();
                DebugEx.ExitMethod();
            }
        }

        public void CancelTransaction()
        {
            // 注文送信中なら送信をキャンセル
            // 注文送信済みならキャンセルを送信
            SendCancelOrderRequestAsync().Wait();
        }

        public void OnParentOrderEvent(BfParentOrderEvent poe)
        {
            if (poe.ChildOrderAcceptanceId != Order.ParentOrderAcceptanceId)
            {
                throw new AggregateException();
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
                    break;

                case BfOrderEventType.Complete:
                case BfOrderEventType.Execution:
                default:
                    throw new NotSupportedException();
            }
        }

        void NotifyEvent(BfxOrderTransactionEventType oet)
        {
            OrderTransactionEvent?.Invoke(this, new BfxParentOrderTransactionEventArgs
            {
                EventType = oet,
                State = State,
                Time = _market.ServerTime,
                Order = Order,
            });
        }

        void NotifyEvent(BfxOrderTransactionEventType oet, BfParentOrderEvent poe)
        {
            OrderTransactionEvent?.Invoke(this, new BfxParentOrderTransactionEventArgs
            {
                EventType = oet,
                State = State,
                Time = poe.EventDate,
                Order = Order,

                OrderEvent = poe,
            });
        }

        public override void OnChildOrderEvent(BfChildOrderEvent coe)
        {
            Order.Update(coe);
        }

        void NotifyEvent(BfxOrderTransactionEventType oet, IBitFlyerResponse resp)
        {
            OrderTransactionEvent?.Invoke(this, new BfxParentOrderTransactionEventArgs
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
