//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
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
    public class BfxParentTransaction : BfxTransaction
    {
        public override string MarketId => _order.AcceptanceId;
        public override IBfxOrder Order => _order;
        public override BfxOrderState OrderState => Order.State;

        protected override void CancelTransaction() => _cts.Cancel();

        // Private properties
        CancellationTokenSource _cts = new CancellationTokenSource();
        BfxParentOrder _order;

        public BfxParentTransaction(BfxMarket market, BfxParentOrder order)
            : base(market)
        {
            _order = order;
        }

        public BfxParentTransaction(BfxMarket2 market, BfxParentOrder order)
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
                ChangeState(BfxTransactionState.SendingOrder);
                NotifyEvent(BfxOrderEventType.OrderSending);
                for (var retry = 0; retry <= Market.Config.OrderRetryMax; retry++)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    var resp = await Market.Client.SendParentOrderAsync(_order.Request, _cts.Token);
                    if (!resp.IsError)
                    {
                        Market.OrderCache?.OpenParentOrder(_order.Request, resp.GetContent());
                        _order.Update(resp.GetContent());
                        ChangeState(BfxTransactionState.WaitingOrderAccepted);
                        NotifyEvent(BfxOrderEventType.OrderSent, Market.ServerTime, resp);
                        Market.RegisterTransaction(this);
                        return;
                    }

                    Log.Warn($"SendParentOrder failed: {resp.StatusCode} {resp.ErrorMessage}");
                    _cts.Token.ThrowIfCancellationRequested();
                    Log.Info("Trying retry...");
                    await Task.Delay(Market.Config.OrderRetryInterval);
                }

                Log.Error("SendOrderRequest - Retried out");
                ChangeState(BfxTransactionState.Idle);
                NotifyEvent(BfxOrderEventType.OrderSendFailed);
                throw new BitFlyerDotNetException();
            }
            catch (OperationCanceledException ex)
            {
                Log.Trace("SendParentOrderRequestAsync is canceled");
                ChangeState(BfxTransactionState.Idle);
                NotifyEvent(BfxOrderEventType.OrderSendCanceled, Market.ServerTime, ex);
            }
        }

        protected override async Task SendCancelOrderRequestAsync()
        {
            if (State == BfxTransactionState.SendingOrder)
            {
                _cts.Token.ThrowIfCancellationRequested();
            }

            ChangeState(BfxTransactionState.SendingCancel);
            NotifyEvent(BfxOrderEventType.CancelSending);
            try
            {
                var resp = await Market.Client.CancelParentOrderAsync(Market.ProductCode, string.Empty, _order.AcceptanceId, _cts.Token);
                if (!resp.IsError)
                {
                    ChangeState(BfxTransactionState.CancelAccepted);
                    NotifyEvent(BfxOrderEventType.CancelSent, Market.ServerTime, resp);
                }
                else
                {
                    ChangeState(BfxTransactionState.Idle);
                    NotifyEvent(BfxOrderEventType.CancelSendFailed, Market.ServerTime, resp);
                }
            }
            catch (OperationCanceledException ex)
            {
                ChangeState(BfxTransactionState.Idle);
                NotifyEvent(BfxOrderEventType.CancelSendCanceled, Market.ServerTime, ex);
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
                    ChangeState(BfxTransactionState.Idle);
                    NotifyEvent(BfxOrderEventType.Ordered, poe);
                    break;

                case BfOrderEventType.OrderFailed:
                    ChangeState(BfxTransactionState.Idle);
                    NotifyEvent(BfxOrderEventType.OrderFailed, poe);
                    break;

                case BfOrderEventType.Cancel:
                    ChangeState(BfxTransactionState.Closed);
                    NotifyEvent(BfxOrderEventType.Canceled, poe);
                    break;

                case BfOrderEventType.CancelFailed:
                    ChangeState(BfxTransactionState.Idle);
                    NotifyEvent(BfxOrderEventType.CancelFailed, poe);
                    break;

                case BfOrderEventType.Trigger:
                    Log.Trace($"Trigger {poe.Side} P:{poe.Price} S:{poe.Size}");
                    break;

                case BfOrderEventType.Expire:
                    ChangeState(BfxTransactionState.Closed);
                    NotifyEvent(BfxOrderEventType.Expired, poe);
                    break;

                case BfOrderEventType.Complete:
                    if (Order.State == BfxOrderState.Completed)
                    {
                        ChangeState(BfxTransactionState.Closed);
                        NotifyEvent(BfxOrderEventType.Completed, poe);
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
                    NotifyChildOrderEvent(BfxOrderEventType.Ordered, childOrderIndex, coe);
                    break;

                case BfOrderEventType.OrderFailed:
                    NotifyChildOrderEvent(BfxOrderEventType.OrderFailed, childOrderIndex, coe);
                    break;

                case BfOrderEventType.Cancel:
                    NotifyChildOrderEvent(BfxOrderEventType.Canceled, childOrderIndex, coe);
                    break;

                case BfOrderEventType.CancelFailed:
                    break;

                case BfOrderEventType.Execution:
                    NotifyChildOrderEvent(childOrder.State == BfxOrderState.PartiallyExecuted ? BfxOrderEventType.PartiallyExecuted : BfxOrderEventType.Executed, childOrderIndex, coe);
                    break;

                case BfOrderEventType.Expire:
                    NotifyChildOrderEvent(BfxOrderEventType.Expired, childOrderIndex, coe);
                    break;

                case BfOrderEventType.Unknown:
                case BfOrderEventType.Complete:
                case BfOrderEventType.Trigger:
                    throw new NotSupportedException();
            }
        }
    }
}
