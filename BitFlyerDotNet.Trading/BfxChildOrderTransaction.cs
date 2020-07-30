//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxChildOrderTransaction : IBfxOrderTransaction
    {
        // Public properties
        public BfxChildOrder Order { get; private set; }
        public IReadOnlyList<BfChildOrderEvent> EventHistory => _eventHistory;
        public BfxParentOrderTransaction? Parent { get; }

        // Events
        public event EventHandler<BfxChildOrderTransactionEventArgs>? OrderTransactionEvent;

        // Private properties
        BfxMarket _market;
        List<BfChildOrderEvent> _eventHistory = new List<BfChildOrderEvent>();

        public BfxChildOrderTransaction(BfxMarket market, BfxChildOrder order, BfxParentOrderTransaction? parent)
        {
            _market = market;
            Order = order;
            Parent = parent;
        }

        public BfxChildOrderTransaction(BfxMarket market, BfxChildOrder order)
        {
            _market = market;
            Order = order;
        }

        // - 経過時間でリトライ終了のオプション
        // - 通信エラー以外でのリトライ終了
        // - 送信完了からChildOrderEvent(Order)までの状態
        public string SendOrderRequest()
        {
            Debug.Assert(Order.Request != null);
            Debug.Assert(Order.State == BfxOrderState.Unknown);
            DebugEx.EnterMethod();
            try
            {
                Order.TransitState(BfxOrderState.SendingOrder);
                for (var retry = 0; retry <= _market.Config.OrderRetryMax; retry++)
                {
                    DebugEx.Trace();
                    var resp = _market.Client.SendChildOrder(Order.Request);
                    if (!resp.IsError)
                    {
                        Order.Update(resp.GetMessage());
                        Order.TransitState(BfxOrderState.WaitingOrderAccepted);
                        NotifyEvent(BfxOrderEventType.OrderSent, resp);
                        return resp.GetMessage().ChildOrderAcceptanceId;
                    }

                    DebugEx.Trace("Trying retry...");
                    Thread.Sleep(_market.Config.OrderRetryInterval);
                }

                DebugEx.Trace("SendOrderRequest - Retried out");
                Order.TransitState(BfxOrderState.Unknown);
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

        // - エラーリトライ(無限リトライ)
        // - 注文執行によるキャンセルの中止 => CancelFailed受信
        // - 注文送信リトライ中のキャンセル
        IBitFlyerResponse SendCancelOrderRequest()
        {
            Debug.Assert(!string.IsNullOrEmpty(Order.ChildOrderAcceptanceId));
            Debug.Assert(Order.State == BfxOrderState.OrderConfirmed);

            DebugEx.EnterMethod();
            Order.TransitState(BfxOrderState.SendingCancel);
            try
            {
                DebugEx.Trace();
                var resp = _market.Client.CancelChildOrder(productCode: _market.ProductCode, childOrderAcceptanceId: Order.ChildOrderAcceptanceId);
                if (resp.IsError)
                {
                    Order.TransitState(BfxOrderState.OrderConfirmed);
                    NotifyEvent(BfxOrderEventType.CancelSendFailed, resp);
                }
                else
                {
                    Order.TransitState(BfxOrderState.WaitingCancelCompleted);
                    NotifyEvent(BfxOrderEventType.CancelSent, resp);
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

        public void CancelTransaction()
        {
            // 注文送信中なら送信をキャンセル
            // 注文送信済みならキャンセルを送信
            SendCancelOrderRequest();
        }

        public void OnChildOrderEvent(BfChildOrderEvent coe)
        {
            if (coe.ChildOrderAcceptanceId != Order.ChildOrderAcceptanceId)
            {
                return; // Event for childrent of parent
            }

            RecordChildOrderEvent(coe);
            Order.Update(coe);
            Order.TransitState(coe.EventType);

            switch (coe.EventType)
            {
                case BfOrderEventType.Order: // Order registered
                    NotifyEvent(BfxOrderEventType.OrderAccepted, coe);
                    break;

                case BfOrderEventType.OrderFailed:
                    NotifyEvent(BfxOrderEventType.OrderFailed, coe);
                    break;

                case BfOrderEventType.Cancel:
                    NotifyEvent(BfxOrderEventType.Canceled, coe);
                    break;

                case BfOrderEventType.CancelFailed:
                    // Reasonが無いが、注文執行済みと考えて良いか？また、Completeは送信されるのか？
                    NotifyEvent(BfxOrderEventType.CancelFailed, coe);
                    break;

                case BfOrderEventType.Execution:
                    NotifyEvent(Order.OrderSize > Order.ExecutedSize ? BfxOrderEventType.PartiallyExecuted : BfxOrderEventType.Executed, coe);
                    break;

                // Not happened when Simple Order ?
                case BfOrderEventType.Trigger:
                    break;

                case BfOrderEventType.Complete:
                    NotifyEvent(BfxOrderEventType.Completed, coe);
                    break;

                // - When TimeInForce=FOK
                case BfOrderEventType.Expire:
                    NotifyEvent(BfxOrderEventType.Expired, coe);
                    break;

                case BfOrderEventType.Unknown:
                    throw new NotSupportedException();
            }
        }

        void RecordChildOrderEvent(BfChildOrderEvent coe)
        {
            _eventHistory.Add(coe);
            if (_eventHistory.Count > 100)
            {
                _eventHistory.RemoveAt(0);
            }
        }

        void NotifyEvent(BfxOrderEventType oet, BfChildOrderEvent coe)
        {
            OrderTransactionEvent?.Invoke(this, new BfxChildOrderTransactionEventArgs { EventType = oet, OrderEvent = coe });
        }

        void NotifyEvent(BfxOrderEventType oet, IBitFlyerResponse resp)
        {
            OrderTransactionEvent?.Invoke(this, new BfxChildOrderTransactionEventArgs { EventType = oet, Response = resp });
        }
    }
}
