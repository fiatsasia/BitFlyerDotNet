//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public class BfxParentOrderTransaction : BfxOrderTransaction
    {
        public override string MarketId => _order.AcceptanceId;
        public override IBfxOrder Order => _order;
        public override BfxOrderState OrderState => Order.State;

        protected override void CancelTransaction() => _cts.Cancel();

        // Private properties
        CancellationTokenSource _cts = new CancellationTokenSource();
        BfxParentOrder _order;

        public BfxParentOrderTransaction(BfxMarket market, BfxParentOrder order)
            : base(market)
        {
            _order = order;
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
                    var resp = await Market.Client.SendParentOrderAsync(_order.Request, _cts.Token);
                    if (!resp.IsError)
                    {
                        Market.OrderCache?.OpenParentOrder(_order.Request, resp.GetContent());
                        _order.Update(resp.GetContent());
                        ChangeState(BfxOrderTransactionState.WaitingOrderAccepted);
                        NotifyEvent(BfxOrderTransactionEventType.OrderSent, Market.ServerTime, resp);
                        Market.RegisterTransaction(this);
                        return;
                    }

                    Log.Warn($"SendParentOrder failed: {resp.StatusCode} {resp.ErrorMessage}");
                    _cts.Token.ThrowIfCancellationRequested();
                    Log.Info("Trying retry...");
                    await Task.Delay(Market.Config.OrderRetryInterval);
                }

                Log.Error("SendOrderRequest - Retried out");
                ChangeState(BfxOrderTransactionState.Idle);
                NotifyEvent(BfxOrderTransactionEventType.OrderSendFailed);
                throw new BitFlyerDotNetException();
            }
            catch (OperationCanceledException ex)
            {
                Log.Trace("SendParentOrderRequestAsync is canceled");
                ChangeState(BfxOrderTransactionState.Idle);
                NotifyEvent(BfxOrderTransactionEventType.OrderSendCanceled, Market.ServerTime, ex);
            }
        }

        protected override async Task SendCancelOrderRequestAsync()
        {
            if (State == BfxOrderTransactionState.SendingOrder)
            {
                _cts.Token.ThrowIfCancellationRequested();
            }

            ChangeState(BfxOrderTransactionState.SendingCancel);
            NotifyEvent(BfxOrderTransactionEventType.CancelSending);
            try
            {
                var resp = await Market.Client.CancelParentOrderAsync(Market.ProductCode, string.Empty, _order.AcceptanceId, _cts.Token);
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

        public void OnParentOrderEvent(BfParentOrderEvent poe)
        {
            if (poe.ParentOrderAcceptanceId != _order.AcceptanceId)
            {
                throw new ApplicationException();
            }

            _order.Update(poe);

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
                    ChangeState(BfxOrderTransactionState.Closed);
                    NotifyEvent(BfxOrderTransactionEventType.Canceled, poe);
                    break;

                case BfOrderEventType.CancelFailed:
                    ChangeState(BfxOrderTransactionState.Idle);
                    NotifyEvent(BfxOrderTransactionEventType.CancelFailed, poe);
                    break;

                case BfOrderEventType.Trigger:
                    Log.Trace($"Trigger {poe.Side} P:{poe.Price} S:{poe.Size}");
                    break;

                case BfOrderEventType.Expire:
                    ChangeState(BfxOrderTransactionState.Closed);
                    NotifyEvent(BfxOrderTransactionEventType.Expired, poe);
                    break;

                case BfOrderEventType.Complete:
                    if (Order.State == BfxOrderState.Completed)
                    {
                        ChangeState(BfxOrderTransactionState.Closed);
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
            var childOrderIndex = _order.Update(coe);
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
                    break;

                case BfOrderEventType.Execution:
                    NotifyChildOrderEvent(childOrder.State == BfxOrderState.PartiallyExecuted ? BfxOrderTransactionEventType.PartiallyExecuted : BfxOrderTransactionEventType.Executed, childOrderIndex, coe);
                    break;

                case BfOrderEventType.Expire:
                    NotifyChildOrderEvent(BfxOrderTransactionEventType.Expired, childOrderIndex, coe);
                    break;

                case BfOrderEventType.Unknown:
                case BfOrderEventType.Complete:
                case BfOrderEventType.Trigger:
                    throw new NotSupportedException();
            }
        }
    }
}
