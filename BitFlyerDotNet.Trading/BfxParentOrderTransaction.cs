//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Reactive.Disposables;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxParentOrderTransaction : IBfxOrderTransaction
    {
        public BfxParentOrder Order { get; private set; }
        public IReadOnlyList<BfParentOrderEvent> EventHistory => _eventHistory;

        // Events
        public event EventHandler<BfxParentOrderTransactionEventArgs>? OrderTransactionEvent;

        BfxMarket _market;
        List<BfParentOrderEvent> _eventHistory = new List<BfParentOrderEvent>();

        public BfxParentOrderTransaction(BfxMarket market, BfxParentOrder order)
        {
            _market = market;
            Order = order;
        }

        public string SendOrderRequest()
        {
            Debug.Assert(Order.Request != null);
            DebugEx.EnterMethod();
            try
            {
                for (var retry = 0; retry <= _market.Config.OrderRetryMax; retry++)
                {
                    DebugEx.Trace();
                    var resp = _market.Client.SendParentOrder(Order.Request);
                    if (!resp.IsError)
                    {
                        Order.Update(resp.GetMessage());
                        return resp.GetMessage().ParentOrderAcceptanceId;
                    }

                    DebugEx.Trace("Trying retry...");
                    Thread.Sleep(_market.Config.OrderRetryInterval);
                }

                DebugEx.Trace("SendOrderRequest - Retried out");
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

        IBitFlyerResponse SendCancelOrderRequest()
        {
            if (string.IsNullOrEmpty(Order.ParentOrderAcceptanceId))
            {
                throw new InvalidOperationException("Not ordered.");
            }

            DebugEx.EnterMethod();
            try
            {
                DebugEx.Trace();
                return _market.Client.CancelParentOrder(productCode: _market.ProductCode, parentOrderAcceptanceId: Order.ParentOrderAcceptanceId);
            }
            catch
            {
                DebugEx.Trace();
                return default;
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
                    break;

                case BfOrderEventType.OrderFailed:
                    break;

                case BfOrderEventType.Cancel:
                    break;

                case BfOrderEventType.CancelFailed:
                    // Reasonが無いが、注文執行済みと考えて良いか？また、Completeは送信されるのか？
                    break;

                case BfOrderEventType.Execution:
                    // 分割約定の場合、Sizeは約定毎か累積か？
                    break;

                case BfOrderEventType.Trigger:
                    // Simple Order では発生しない？
                    break;

                case BfOrderEventType.Complete:
                    // Executionと別に送信されるのか？
                    break;

                case BfOrderEventType.Expire:
                    break;

                default:
                    throw new NotSupportedException();
            }

            _eventHistory.Add(poe);
        }

        public void OnChildOrderEvent(BfChildOrderEvent coe)
        {
            Order.Update(coe);
        }
    }
}
